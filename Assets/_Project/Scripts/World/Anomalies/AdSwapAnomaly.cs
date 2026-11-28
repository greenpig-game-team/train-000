using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 이상현상: 광고판 교체. 칸의 광고판 중 하나를 다른 광고로 바꿔치기.
    /// anomalySprites가 비어 있으면(아트 나오기 전) placeholder로 원래 광고를 좌우반전 + 붉게 표시.
    /// </summary>
    public class AdSwapAnomaly : AnomalyBase
    {
        [Tooltip("바뀐 광고 이미지 (랜덤 선택). 아트팀 광고 나오면 여기에 추가")]
        [SerializeField] Sprite[] anomalySprites;
        [SerializeField] Color placeholderTint = new(1f, 0.55f, 0.55f, 1f);

        AdBoard target;

        void Reset() => SetId("ad_swap");

        public override bool CanApplyTo(TrainCar car) =>
            base.CanApplyTo(car) && car.AdBoards != null && car.AdBoards.Length > 0;

        protected override void OnActivate(TrainCar car)
        {
            target = car.AdBoards[Random.Range(0, car.AdBoards.Length)];
            if (anomalySprites != null && anomalySprites.Length > 0)
                target.Swap(anomalySprites[Random.Range(0, anomalySprites.Length)]);
            else
                target.SetPlaceholderAnomaly(true, placeholderTint);
        }

        protected override void OnDeactivate(TrainCar car)
        {
            if (target == null) return;
            target.Restore();
            target = null;
        }
    }
}
