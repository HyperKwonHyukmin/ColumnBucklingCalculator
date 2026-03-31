using System;
using ColumnBucklingApp.Models;

namespace ColumnBucklingApp.Services
{
  /// <summary>
  /// AISC 설계 기준을 바탕으로 기둥의 탄성/비탄성 좌굴 임계응력 및 최대 사용하중을 계산하는 서비스입니다.
  /// </summary>
  public class BucklingCalculatorService
  {
    private const double GravityForce = 9810.0; // N을 ton으로 변환하기 위한 상수

    /// <summary>
    /// 입력된 기둥 제원과 편심량을 바탕으로 최대 허용 사용하중(ton)을 계산합니다.
    /// </summary>
    /// <param name="input">계산에 필요한 부재 및 하중 입력 정보</param>
    /// <returns>안전율이 적용된 사용하중 [ton]</returns>
    public double CalculateWorkingLoad(BucklingInput input)
    {
      var m = input.Member;

      // 1. 편심 기준거리 및 편심량(e) 계산 (엑셀 IF 로직 대체)
      double refDistance = m.IsIASection ? (m.ReferenceDim - m.CentroidY) : (m.ReferenceDim / 2.0);
      double eccentricity = input.EccentricityRatio * refDistance;

      // 2. 탄성 좌굴임계 응력 (Fe) 계산 (오일러식 기반)
      // 식: PI^2 * E * I / (L^2 * A)
      double Fe = Math.Pow(Math.PI, 2) * input.ElasticModulus * m.MomentOfInertia
                  / (Math.Pow(input.Length, 2) * m.Area);

      // 3. 비탄성 좌굴임계 응력 (Fcr) 계산 (AISC 기반)
      double slendernessRatio = input.Length / m.RadiusOfGyration; // KL/r (K=1)
      double limitRatio = 4.71 * Math.Sqrt(input.ElasticModulus / input.YieldStress);

      double Fcr = (slendernessRatio <= limitRatio)
          ? Math.Pow(0.658, input.YieldStress / Fe) * input.YieldStress
          : 0.877 * Fe;

      // 4. 허용 사용 하중 계산 로직
      if (eccentricity == 0)
      {
        // 편심이 없을 경우 (e = 0)
        return (Fcr * m.Area) / GravityForce / input.SafetyFactor;
      }
      else
      {
        // 편심이 있을 경우 (Secant Formula 적용)
        double maxWorkingLoad = 0;

        // [기존 10% 단위에서 0.5% 단위로 정밀도 20배 향상]
        // ratio를 100%(1.0)부터 0.1%(0.001)까지 0.5%(0.001)씩 깎아내리며 검사합니다.
        for (double ratio = 1.0; ratio >= 0.001; ratio -= 0.001)
        {
          double P = Fcr * m.Area * ratio; // 비탄성 좌굴임계 하중

          // Secant 항 계산: SEC(L/(2r) * SQRT(P/(A*E)))
          double secantInput = (input.Length / (2 * m.RadiusOfGyration)) * Math.Sqrt(P / (m.Area * input.ElasticModulus));
          double secantTerm = 1.0 / Math.Cos(secantInput);

          // 최대 응력(sigma_max) 계산
          double sigmaMax = (P / m.Area) * (1 + (m.Area * eccentricity / m.SectionModulus) * secantTerm);

          // 항복응력(Fy) 이내인지 검증 ("OK" 조건)
          if (sigmaMax <= input.YieldStress)
          {
            // 위에서부터(100%) 하중을 내리며 확인했으므로, 
            // 처음으로 조건을 만족하는 이 순간의 하중이 '최대 허용 하중'입니다.
            maxWorkingLoad = P / GravityForce / input.SafetyFactor;

            // 값을 찾았으므로 더 이상 불필요한 계산을 하지 않고 즉시 반복문을 탈출합니다 (연산 최적화).
            break;
          }
        }

        return maxWorkingLoad;
      }
    }
  }
}