using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CardDataLoader : MonoBehaviour
{
    [Header("리소스에 있는 SchottenTotten_DataTables_CardData.CSV")]
    public TextAsset CsvFile;

    public List<CardData> LoadCardDataList()
    {
        var cardDataList = new List<CardData>();
        if (CsvFile == null)
        {
            Debug.LogError("[CardDataLoader] CSV 파일이 설정되지 않았습니다.");
            return cardDataList;
        }

        string[] lines = CsvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 첫 줄은 헤더
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');

            if (values.Length < 4) continue;

            var data = new CardData
            {
                ID = values[0].Trim(),
                Color = values[1].Trim(),
                Number = int.Parse(values[2]),
                ImageName = values[3].Trim()
            };

            cardDataList.Add(data);
        }

        Debug.Log($"[CardDataLoader] {cardDataList.Count}장의 카드 데이터를 불러왔습니다.");
        return cardDataList;
    }
}