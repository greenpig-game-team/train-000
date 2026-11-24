
using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    private bool isOpeningDialogue = false;
    public OpeningManager openingManager;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerName;
    public GameObject dialoguePanel;

    public GameObject portraitImage;
    public GameObject choiceButton1;
    public GameObject choiceButton2;


    public float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private DialogueLine[] currentDialogue;
    private int currentIndex = 0;
    private bool isTyping = false;

    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string sentence;

        public DialogueLine(string speaker, string sentence)
        {
            this.speaker = speaker;
            this.sentence = sentence;
        }
    }

    private void Start()
    {
        // 오프닝 독백은 날짜 연출 이후에 시작할 예정
        // StartOpening();
    }

    public void SelectChoice1()
    {
        StartDialogue(new DialogueLine[]
        {
            new DialogueLine("승객", "네, 여기 제 승차권입니다."),
            new DialogueLine("주인공", "확인되었습니다."),
            new DialogueLine("승객", "감사합니다.")
        });
    }

    public void SelectChoice2()
    {
        StartDialogue(new DialogueLine[]
        {
            new DialogueLine("승객", "지금은 승차권이 없습니다."),
            new DialogueLine("주인공", "그렇다면 다시 확인하겠습니다.")
        });
    }


    

    public void StartOpening()
    {
        isOpeningDialogue = true;
        portraitImage.SetActive(false);
        choiceButton1.SetActive(false);
        choiceButton2.SetActive(false);
        speakerName.gameObject.SetActive(false);

        StartDialogue(new DialogueLine[]
        {
            new DialogueLine(
                "",
                "벌써 며칠째 초과근무중인지 모르겠다."
            ),

            new DialogueLine(
                "",
                "급료를 올려달라고 해야겠다."
            ),

            new DialogueLine(
                "",
                "야간 근무 X일째."
            ),

            new DialogueLine(
                "",
                "오늘 운행만 끝나면 쉬게 해주겠다는 말을 들은 지 벌써 며칠이 지났는지 모르겠다."
            ),

            new DialogueLine(
                "",
                "이제는 집에 대한 기억조차 희미하다."
            )
        });
    }




    public void StartPassengerDialogue()
    {
        isOpeningDialogue = false;
        
        // 일반 대화에서는 캐릭터와 이름 표시
        portraitImage.SetActive(true);
        speakerName.gameObject.SetActive(true);

        // 선택지 버튼 표시
        choiceButton1.SetActive(true);
        choiceButton2.SetActive(true);

        // 승객 대화 시작
        StartDialogue(new DialogueLine[]
        {
            new DialogueLine("승객", "안녕하세요. 무슨 일이신가요?"),
            new DialogueLine("주인공", "승차권을 확인하겠습니다."),
            new DialogueLine("승객", "네, 알겠습니다.")
        });
    }



    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        // 대화가 시작되면 UI 표시
        dialoguePanel.SetActive(true);

        currentDialogue = lines;
        currentIndex = 0;

        ShowCurrentLine();
    }


    private void ShowCurrentLine()
    {
        DialogueLine line = currentDialogue[currentIndex];

        speakerName.text = line.speaker;
        StartTyping(line.sentence);
    }

    private void StartTyping(string sentence)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = sentence;
        dialogueText.maxVisibleCharacters = 0;

        isTyping = true;
        typingCoroutine = StartCoroutine(TypeSentence());
    }

    private IEnumerator TypeSentence()
    {
        dialogueText.ForceMeshUpdate();

        int totalCharacters =
            dialogueText.textInfo.characterCount;

        for (int i = 0; i < totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void NextDialogue()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogueText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        currentIndex++;

        if (currentDialogue != null &&
            currentIndex < currentDialogue.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

private void EndDialogue()
    {
        dialogueText.text = "";
        speakerName.text = "";

        isTyping = false;
        typingCoroutine = null;
        currentDialogue = null;
        currentIndex = 0;

        dialoguePanel.SetActive(false);

        Debug.Log("대화 종료");


        Debug.Log("오프닝 여부: " + isOpeningDialogue);
        Debug.Log("OpeningManager 연결 여부: " + (openingManager != null));

        if (isOpeningDialogue && openingManager != null)
        {
            Debug.Log("탑승 안내 표시 요청!");
            openingManager.ShowBoardingPrompt();
        }

        isOpeningDialogue = false;

    }


}
