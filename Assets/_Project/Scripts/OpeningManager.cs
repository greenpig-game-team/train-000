
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class OpeningManager : MonoBehaviour
{
    public TextMeshProUGUI openingDateText;

    public GameObject openingPanel;
    public GameObject stationPanel;
    public GameObject boardingText;

    public GameObject trainDoorImage;

    public DialogueManager dialogueManager;

    public GameObject stationPlaceholderText;
    public float fadeDuration = 2f;
    public float holdDuration = 1.5f;

    private bool canBoard = false;

    public GameObject doorOpeningText;
    public float doorOpeningDuration = 2f;

    
    private void Start()
    {
        // 날짜 연출 화면 표시
        openingPanel.SetActive(true);

        // 독백 배경 준비
        stationPanel.SetActive(false);

        // 열차 문과 탑승 안내는 처음에 숨김
        trainDoorImage.SetActive(false);
        boardingText.SetActive(false);

        canBoard = false;

        StartCoroutine(PlayDateOpening());
    }


    private IEnumerator PlayDateOpening()
    {
        Color color = openingDateText.color;
        color.a = 0f;
        openingDateText.color = color;

        float elapsed = 0f;

        // 날짜 페이드 인
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            openingDateText.color = color;

            yield return null;
        }

        color.a = 1f;
        openingDateText.color = color;

        yield return new WaitForSeconds(holdDuration);

        // 날짜 페이드 아웃
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            openingDateText.color = color;

            yield return null;
        }

        color.a = 0f;
        openingDateText.color = color;

        // 검은 화면 숨기기
        openingPanel.SetActive(false);

        // 정거장 화면 표시
        stationPanel.SetActive(true);

        // 주인공 독백 시작
        dialogueManager.StartOpening();
    }


public void ShowBoardingPrompt()
{
    Debug.Log("문 열림 연출 시작!");

    StartCoroutine(PlayDoorOpening());
}

private IEnumerator PlayDoorOpening()
{
    stationPlaceholderText.SetActive(false);
    trainDoorImage.SetActive(false);
    boardingText.SetActive(false);
    canBoard = false;

    doorOpeningText.SetActive(true);
    Debug.Log("문 열림 텍스트 표시!");

    yield return new WaitForSeconds(doorOpeningDuration);

    doorOpeningText.SetActive(false);
    trainDoorImage.SetActive(true);
    boardingText.SetActive(true);
    canBoard = true;

    Debug.Log("열차 이미지 및 E키 안내 표시!");
}

    private void Update()
    {
        if (canBoard &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            BoardTrain();
        }
    }

    private void BoardTrain()
    {
        canBoard = false;
        boardingText.SetActive(false);

        Debug.Log("열차 탑승 성공!");
    }
}
