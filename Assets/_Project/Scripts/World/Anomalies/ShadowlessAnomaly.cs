using System.Collections.Generic;
using UnityEngine;

namespace GreenPig.World
{
    /// <summary>
    /// 이상현상: 그림자 없는 승객. 칸 안의 CarShadow 중 하나를 숨김.
    /// 칸 안에 CarShadow가 없으면 이 칸에는 배정되지 않음.
    /// </summary>
    public class ShadowlessAnomaly : AnomalyBase
    {
        CarShadow target;
        static readonly List<CarShadow> buffer = new();

        void Reset() => SetId("no_shadow");

        public override bool CanApplyTo(TrainCar car) => base.CanApplyTo(car) && Collect(car) > 0;

        protected override void OnActivate(TrainCar car)
        {
            int n = Collect(car);
            if (n == 0) return;
            target = buffer[Random.Range(0, n)];
            target.SetVisible(false);
        }

        protected override void OnDeactivate(TrainCar car)
        {
            if (target == null) return;
            target.SetVisible(true);
            target = null;
        }

        static int Collect(TrainCar car)
        {
            buffer.Clear();
            foreach (var s in CarShadow.All)
                if (s != null && car.ContainsX(s.transform.position.x)) buffer.Add(s);
            return buffer.Count;
        }
    }
}
