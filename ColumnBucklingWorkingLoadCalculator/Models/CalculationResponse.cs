namespace ColumnBucklingApp.Models;

public class CalculationResponse
{
  public InputSummary? Input { get; set; }
  public MemberProfileSummary? MemberProfile { get; set; }
  public IntermediateValues? IntermediateValues { get; set; }
  public CalculationResult? Result { get; set; }
  public ErrorInfo? Error { get; set; }
}

public class InputSummary
{
  public string MemberName { get; set; } = "";
  public double ColumnLengthMm { get; set; }
  public double EccentricityRatio { get; set; }
  public double ElasticModulusMpa { get; set; }
  public double YieldStressMpa { get; set; }
  public double SafetyFactor { get; set; }
}

public class MemberProfileSummary
{
  public string Name { get; set; } = "";
  public double AreaMm2 { get; set; }
  public double MomentOfInertiaMm4 { get; set; }
  public double RadiusOfGyrationMm { get; set; }
  public double SectionModulusMm3 { get; set; }
  public double ReferenceDimMm { get; set; }
  public double CentroidYMm { get; set; }
  public bool IsIASection { get; set; }
}

public class IntermediateValues
{
  public double EccentricityMm { get; set; }
  public double EulerStressFe { get; set; }
  public double SlendernessRatio { get; set; }
  public double SlendernessLimit { get; set; }
  public double CriticalStressFcr { get; set; }
  public double? LoadRatio { get; set; }
}

public class CalculationResult
{
  public double MaxWorkingLoadTon { get; set; }
  public string LoadCase { get; set; } = "";
}

public class ErrorInfo
{
  public string Code { get; set; } = "";
  public string Message { get; set; } = "";
}
