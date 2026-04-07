namespace ColumnBucklingApp.Models;

public class CalculationRequest
{
  public required string MemberName { get; set; }
  public double ColumnLengthMm { get; set; }
  public double EccentricityRatio { get; set; }
}
