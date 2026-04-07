using ColumnBucklingApp.Models;
using ColumnBucklingApp.Services;
using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ColumnBucklingApp
{
  class Program
  {
    private const double ElasticModulus = 210000;
    private const double YieldStress = 240;
    private const double SafetyFactor = 3.0;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static int Main(string[] args)
    {
      if (args.Length != 2)
      {
        Console.Error.WriteLine("Usage: ColumnBucklingApp.exe input.json output.json");
        return 1;
      }

      string inputPath = args[0];
      string outputPath = args[1];

      // 데이터베이스 로드
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      var dbService = new MemberDatabaseService();
      string dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PropertyRefer.txt");

      try
      {
        dbService.LoadFromTextFile(dataFilePath);
      }
      catch (Exception ex)
      {
        return WriteError(outputPath, "DATABASE_LOAD_FAILED", ex.Message);
      }

      // 입력 JSON 읽기
      CalculationRequest request;
      try
      {
        string json = File.ReadAllText(inputPath);
        request = JsonSerializer.Deserialize<CalculationRequest>(json, JsonOptions)
          ?? throw new InvalidOperationException("JSON 역직렬화 결과가 null입니다.");
      }
      catch (FileNotFoundException)
      {
        return WriteError(outputPath, "INPUT_FILE_NOT_FOUND", $"입력 파일을 찾을 수 없습니다: {inputPath}");
      }
      catch (Exception ex)
      {
        return WriteError(outputPath, "INVALID_INPUT_JSON", ex.Message);
      }

      // 입력값 검증
      if (string.IsNullOrWhiteSpace(request.MemberName))
        return WriteError(outputPath, "INVALID_INPUT_VALUES", "memberName이 비어 있습니다.");
      if (request.ColumnLengthMm <= 0)
        return WriteError(outputPath, "INVALID_INPUT_VALUES", "columnLengthMm은 0보다 커야 합니다.");
      if (request.EccentricityRatio < 0)
        return WriteError(outputPath, "INVALID_INPUT_VALUES", "eccentricityRatio는 0 이상이어야 합니다.");

      // 부재 조회
      MemberProfile member;
      try
      {
        member = dbService.GetProfile(request.MemberName);
      }
      catch (Exception ex)
      {
        return WriteError(outputPath, "MEMBER_NOT_FOUND", ex.Message);
      }

      // 계산
      var input = new BucklingInput
      {
        Member = member,
        SafetyFactor = SafetyFactor,
        ElasticModulus = ElasticModulus,
        YieldStress = YieldStress,
        Length = request.ColumnLengthMm,
        EccentricityRatio = request.EccentricityRatio
      };

      var calculator = new BucklingCalculatorService();
      double workingLoad;
      IntermediateValues intermediates;
      string loadCase;

      try
      {
        (workingLoad, intermediates, loadCase) = calculator.CalculateWorkingLoadDetailed(input);
      }
      catch (Exception ex)
      {
        return WriteError(outputPath, "CALCULATION_FAILED", ex.Message);
      }

      // 응답 조립
      var response = new CalculationResponse
      {
        Input = new InputSummary
        {
          MemberName = request.MemberName,
          ColumnLengthMm = request.ColumnLengthMm,
          EccentricityRatio = request.EccentricityRatio,
          ElasticModulusMpa = ElasticModulus,
          YieldStressMpa = YieldStress,
          SafetyFactor = SafetyFactor
        },
        MemberProfile = new MemberProfileSummary
        {
          Name = member.Name,
          AreaMm2 = member.Area,
          MomentOfInertiaMm4 = member.MomentOfInertia,
          RadiusOfGyrationMm = member.RadiusOfGyration,
          SectionModulusMm3 = member.SectionModulus,
          ReferenceDimMm = member.ReferenceDim,
          CentroidYMm = member.CentroidY,
          IsIASection = member.IsIASection
        },
        IntermediateValues = intermediates,
        Result = new CalculationResult
        {
          MaxWorkingLoadTon = Math.Round(workingLoad, 1),
          LoadCase = loadCase
        },
        Error = null
      };

      File.WriteAllText(outputPath, JsonSerializer.Serialize(response, JsonOptions));
      return 0;
    }

    private static int WriteError(string outputPath, string code, string message)
    {
      var response = new CalculationResponse { Error = new ErrorInfo { Code = code, Message = message } };
      try
      {
        File.WriteAllText(outputPath, JsonSerializer.Serialize(response, JsonOptions));
      }
      catch
      {
        Console.Error.WriteLine($"[{code}] {message}");
      }
      return 1;
    }
  }
}
