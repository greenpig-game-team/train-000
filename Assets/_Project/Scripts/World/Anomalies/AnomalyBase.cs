using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 관찰형 이상현상의 공통 부모. 씬의 "Anomalies" 오브젝트 아래에 컴포넌트로 하나씩 붙여두면
    /// RoundManager가 칸마다 하나씩 골라 Activate(칸) → 판정이 끝나면 Deactivate()로 원상복구.
    /// 한 번에 하나의 칸에만 켜지므로, 같은 이상현상을 다른 칸에서 다시 써도 됨.
    /// 새 이상현상 = 이 클래스를 상속해서 OnActivate / OnDeactivate만 구현.
    /// </summary>
    public abstract class AnomalyBase : MonoBehaviour
    {
        [Tooltip("StageConfig.anomalyIds에서 쓰는 id (예: ad_swap, no_shadow)")]
        [SerializeField] string id;

        public string Id => string.IsNullOrEmpty(id) ? GetType().Name : id;
        public TrainCar ActiveCar { get; private set; }
        public bool IsActive => ActiveCar != null;

        /// <summary>이 칸에 적용할 수 있는지 (예: 광고판이 없는 칸이면 false)</summary>
        public virtual bool CanApplyTo(TrainCar car) => car != null && !car.IsReference;

        public void Activate(TrainCar car)
        {
            if (IsActive) Deactivate();
            ActiveCar = car;
            OnActivate(car);
            Debug.Log($"[Anomaly] {Id} → {car.Index}번 칸");
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            OnDeactivate(ActiveCar);
            ActiveCar = null;
        }

        protected abstract void OnActivate(TrainCar car);
        protected abstract void OnDeactivate(TrainCar car);

        protected void SetId(string newId) => id = newId;
    }
}
