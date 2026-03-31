using ColumnBucklingApp.Models;
using ColumnBucklingApp.Services;
using System;
using System.IO;
using System.Text;

namespace ColumnBucklingApp
{
  public struct AvailableMembers
  {
    public const string H200x200 = "H200x200";
    // ... (나머지 구조체 내용은 기존과 동일하므로 생략하지 않고 그대로 둡니다)
    public const string H300x305 = "H300x305";
    public const string Pipe300A = "300A PIPE";
    public const string Pipe150A_40 = "150A PIPE (#40)";
    public const string Pipe150A_60 = "150A PIPE (#60)";
    public const string Pipe200A_40 = "200A PIPE (#40)";
    public const string Pipe200A_60 = "200A PIPE (#60)";
    public const string Pipe250A_40 = "250A PIPE (#40)";
    public const string Pipe250A_60 = "250A PIPE (#60)";
    public const string Pipe400A_40 = "400A PIPE (#40)";
    public const string Pipe400A_60 = "400A PIPE (#60)";
    public const string IA150 = "150 I.A (150x90x9/12)";
    public const string IA200 = "200 I.A (200x90x9/14)";
    public const string IA250 = "250 I.A (250x90x10/15)";
    public const string I400 = "I400x150x12.5x25";
    public const string I350_24 = "I350x150x12x24";
    public const string I350_15 = "I350x150x9x15";
  }

  class Program
  {
    static void Main(string[] args)
    {
      // 1. [인코딩 해결] .NET Core/5+ 환경에서 한글 인코딩(949) 지원을 위해 등록
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      Console.OutputEncoding = Encoding.GetEncoding(949);

      // 2. 데이터베이스 로드 (실행 파일 위치 기준)
      var dbService = new MemberDatabaseService();
      string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
      string dataFilePath = Path.Combine(baseDirectory, "PropertyRefer.txt");

      try
      {
        dbService.LoadFromTextFile(dataFilePath);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[오류] 데이터 로드 실패: {ex.Message}");
        return;
      }

      // 3. 입력 인자 파싱
      string targetName = AvailableMembers.Pipe300A;
      double length = 4470;
      double eccentricity = 0.25;

      if (args.Length > 0) targetName = args[0];
      if (args.Length > 1 && double.TryParse(args[1], out double pL)) length = pL;
      if (args.Length > 2 && double.TryParse(args[2], out double pE))
      {
        eccentricity = pE / 100.0;
      }

      MemberProfile selectedMember;
      try
      {
        selectedMember = dbService.GetProfile(targetName);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[오류] {ex.Message}");
        return;
      }

      // 4. 좌굴 계산 및 결과 출력
      var input = new BucklingInput
      {
        Member = selectedMember,
        SafetyFactor = 3.0,
        ElasticModulus = 210000,
        YieldStress = 240,
        Length = length,
        EccentricityRatio = eccentricity
      };

      var calculatorService = new BucklingCalculatorService();

      try
      {
        double workingLoad = calculatorService.CalculateWorkingLoad(input);

        // 파이썬 서버가 파싱하기 좋게 정리된 출력부
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("[입력 정보 및 제원 요약]");
        Console.WriteLine($"- 선택된 부재   : {input.Member.Name}");
        Console.WriteLine($"- 단면적(A)     : {input.Member.Area:F2}");
        Console.WriteLine($"- 관성모멘트(I) : {input.Member.MomentOfInertia:F2}");
        Console.WriteLine($"- 회전반경(r)   : {input.Member.RadiusOfGyration:F2}");
        Console.WriteLine($"- 단면계수(Z)   : {input.Member.SectionModulus:F2}");
        Console.WriteLine($"- 탄성계수(E)   : {input.ElasticModulus:F0}");
        Console.WriteLine($"- 항복응력(Fy)  : {input.YieldStress:F0}");
        Console.WriteLine($"- 기둥 길이(L)  : {input.Length}");
        Console.WriteLine($"- 편심 비율(%)  : {input.EccentricityRatio*100}");
        Console.WriteLine("-----------------------------------------------");

        // 🚨 [요청 사항 반영] :F3에서 :F1로 변경하여 소수점 한 자리까지 표현
        Console.WriteLine("\n[최종 계산 결과]");
        Console.WriteLine($"=> 최대 허용 사용하중 : {workingLoad:F1}");
        Console.WriteLine("-----------------------------------------------");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[오류] 계산 중 문제가 발생했습니다: {ex.Message}");
      }
    }
  }

}