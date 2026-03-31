namespace ColumnBucklingApp.Models
{
  /// <summary>
  /// '좌굴계산_pin_pin' 탭의 사용자 입력값을 담는 클래스입니다.
  /// </summary>
  public class BucklingInput
  {
    public double SafetyFactor { get; set; } = 3.0;     // 사용하중 안전율
    public MemberProfile Member { get; set; }           // 선택된 부재
    public double ElasticModulus { get; set; } = 210000;// 탄성계수 E [MPa]
    public double YieldStress { get; set; } = 240;      // 항복응력 Fy [MPa]
    public double Length { get; set; }                  // 기둥 길이 L [mm]
    public double EccentricityRatio { get; set; }       // 편심 기준 입력 (예: 0.25)
  }
}