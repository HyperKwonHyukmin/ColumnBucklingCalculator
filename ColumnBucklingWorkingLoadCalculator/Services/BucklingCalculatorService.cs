using System;
using ColumnBucklingApp.Models;

namespace ColumnBucklingApp.Services
{
  public class BucklingCalculatorService
  {
    private const double GravityForce = 9810.0;

    public (double workingLoad, IntermediateValues intermediates, string loadCase) CalculateWorkingLoadDetailed(BucklingInput input)
    {
      var m = input.Member;

      // 1. 편심 기준거리 및 편심량(e) 계산
      double refDistance = m.IsIASection ? (m.ReferenceDim - m.CentroidY) : (m.ReferenceDim / 2.0);
      double eccentricity = input.EccentricityRatio * refDistance;

      // 2. 탄성 좌굴임계 응력 (Fe) 계산 (오일러식)
      double Fe = Math.Pow(Math.PI, 2) * input.ElasticModulus * m.MomentOfInertia
                  / (Math.Pow(input.Length, 2) * m.Area);

      // 3. 비탄성 좌굴임계 응력 (Fcr) 계산 (AISC)
      double slendernessRatio = input.Length / m.RadiusOfGyration;
      double limitRatio = 4.71 * Math.Sqrt(input.ElasticModulus / input.YieldStress);

      double Fcr = (slendernessRatio <= limitRatio)
          ? Math.Pow(0.658, input.YieldStress / Fe) * input.YieldStress
          : 0.877 * Fe;

      // 4. 허용 사용 하중 계산
      if (Math.Abs(eccentricity) < 1e-10)
      {
        var intermediates = new IntermediateValues
        {
          EccentricityMm = eccentricity,
          EulerStressFe = Fe,
          SlendernessRatio = slendernessRatio,
          SlendernessLimit = limitRatio,
          CriticalStressFcr = Fcr,
          LoadRatio = null
        };
        double workingLoad = (Fcr * m.Area) / GravityForce / input.SafetyFactor;
        return (workingLoad, intermediates, "concentric");
      }
      else
      {
        for (double ratio = 1.0; ratio >= 0.001; ratio -= 0.001)
        {
          double P = Fcr * m.Area * ratio;
          double secantInput = (input.Length / (2 * m.RadiusOfGyration)) * Math.Sqrt(P / (m.Area * input.ElasticModulus));
          double secantTerm = 1.0 / Math.Cos(secantInput);
          double sigmaMax = (P / m.Area) * (1 + (m.Area * eccentricity / m.SectionModulus) * secantTerm);

          if (sigmaMax <= input.YieldStress)
          {
            var intermediates = new IntermediateValues
            {
              EccentricityMm = eccentricity,
              EulerStressFe = Fe,
              SlendernessRatio = slendernessRatio,
              SlendernessLimit = limitRatio,
              CriticalStressFcr = Fcr,
              LoadRatio = ratio
            };
            double workingLoad = P / GravityForce / input.SafetyFactor;
            return (workingLoad, intermediates, "eccentric");
          }
        }

        throw new InvalidOperationException(
          $"편심 Secant 반복 계산 실패: 전체 하중 범위(0.1%~100%)에서 항복응력 조건을 만족하는 하중을 찾지 못했습니다. " +
          $"(부재: {m.Name}, L={input.Length}, e={eccentricity:F4})");
      }
    }
  }
}
