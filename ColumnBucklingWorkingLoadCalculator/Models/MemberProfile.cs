namespace ColumnBucklingApp.Models
{
  /// <summary>
  /// '부재치수' 탭의 단면 제원 데이터를 담는 데이터 모델입니다.
  /// </summary>
  public class MemberProfile
  {
    public string Name { get; set; }
    public double Area { get; set; }             // 단면적 A [mm^2]
    public double MomentOfInertia { get; set; }  // 관성모멘트 I [mm^4]
    public double RadiusOfGyration { get; set; } // 회전반경 r [mm]
    public double SectionModulus { get; set; }   // 단면계수 Z [mm^3]
    public double ReferenceDim { get; set; }     // B or 내경 [mm]
    public double CentroidY { get; set; }        // c_y (도심) [mm]

    /// <summary>
    /// 엑셀의 ISNUMBER(SEARCH(".", C5)) 로직을 대체합니다. 
    /// 부재 이름에 "."이 포함되어 있으면 I.A 단면으로 간주합니다.
    /// </summary>
    public bool IsIASection => Name.Contains(".");
  }
}