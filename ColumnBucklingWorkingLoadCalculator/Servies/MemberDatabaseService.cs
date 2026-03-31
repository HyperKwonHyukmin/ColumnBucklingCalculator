using System;
using System.Collections.Generic;
using System.IO;
using ColumnBucklingApp.Models;

namespace ColumnBucklingApp.Services
{
  /// <summary>
  /// '부재치수2.txt' 파일을 읽어들여 부재 데이터를 메모리에 적재하는 서비스입니다.
  /// 엑셀의 VLOOKUP 기능을 완벽하게 대체합니다.
  /// </summary>
  public class MemberDatabaseService
  {
    private readonly Dictionary<string, MemberProfile> _database = new Dictionary<string, MemberProfile>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 텍스트 파일을 읽어 H형, Pipe, I.A, I형 단면 데이터를 동적으로 파싱합니다.
    /// </summary>
    public void LoadFromTextFile(string filePath)
    {
      if (!File.Exists(filePath))
        throw new FileNotFoundException($"'{filePath}' 파일을 찾을 수 없습니다.");

      var lines = File.ReadAllLines(filePath);

      foreach (var line in lines)
      {
        // 탭(\t)을 기준으로 데이터 분리 (빈 항목 제외)
        var cols = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

        // 단면계수 Zy가 위치한 최소 10개의 데이터가 없으면 헤더이거나 빈 줄이므로 건너뜀
        if (cols.Length < 10) continue;

        string name = cols[0].Trim();

        // "H (mm)", "외경 D", "A (mm)" 등의 헤더(제목) 줄은 무시
        if (name.Contains("mm") || name.Contains("단면적") || name.Contains("도심"))
          continue;

        // 문자열을 숫자로 변환하는 로컬 헬퍼 함수
        double ParseValue(string val)
        {
          if (double.TryParse(val.Trim(), out double result)) return result;
          return 0.0;
        }

        // 부재치수2.txt의 열 구조에 맞춘 인덱스 매핑 (VLOOKUP 대체)
        var profile = new MemberProfile
        {
          Name = name,
          ReferenceDim = ParseValue(cols[2]),     // 3번째 열: B or 내경 d
          Area = ParseValue(cols[3]),             // 4번째 열: 단면적 A
          MomentOfInertia = ParseValue(cols[5]),  // 6번째 열: 관성모멘트 Iy (약축)
          RadiusOfGyration = ParseValue(cols[7]), // 8번째 열: 회전반경 ry
          SectionModulus = ParseValue(cols[9]),   // 10번째 열: 단면계수 Zy
          CentroidY = 0                           // 기본값 0
        };

        // I.A 단면의 경우 도심 c_y가 12번째 열(인덱스 11)에 존재함
        if (cols.Length >= 12)
        {
          profile.CentroidY = ParseValue(cols[11]);
        }

        _database[name] = profile;
      }
    }

    public MemberProfile GetProfile(string name)
    {
      if (_database.TryGetValue(name, out var profile))
        return profile;

      throw new KeyNotFoundException($"'{name}' 부재를 찾을 수 없습니다. 텍스트 파일에 등록된 이름을 확인하세요.");
    }
  }
}