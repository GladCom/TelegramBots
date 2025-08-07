using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace DirectumCareerNightBot.GoogleSheets;

public class GoogleSheetsManager
{
    public GoogleCredential credential;
    public readonly string[] scopes = [SheetsService.Scope.Spreadsheets];
    public static SheetsService service;

    public const string SpreadSheetId = "1MJ81r3eO9YnksjXdKU_U5j3_bj02jHqlLPavljhfzaY";
    
    public void AddUserToInterviewSheet(string fullname, string contact, string experience, string telegramName)
    {
        var range = $"{GoogleSheets.Interview}!A:E";
        var valueRange = new ValueRange();
        var objectList = new List<object>() { fullname, contact, experience, telegramName };

        valueRange.Values = new List<IList<object>> { objectList };

        var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadSheetId, range);

        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        _ = appendRequest.Execute();
    }

    public void AddUserToMatureSheet(string fullname, string contact, string currentWork, string iTExperience, string telegramName)
    {
        var range = $"{GoogleSheets.Mature}!A:E";
        var valueRange = new ValueRange();
        var objectList = new List<object>() { fullname, contact, currentWork, iTExperience, telegramName };

        valueRange.Values = new List<IList<object>> { objectList };

        var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadSheetId, range);

        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        _ = appendRequest.Execute();
    }
    
    public void AddUserToTraineeSheet(string fullname, string contact, string direction, string experience, string telegramName)
    {
        var range = $"{GoogleSheets.Trainee}!A:E";
        var valueRange = new ValueRange();
        var objectList = new List<object>() { fullname, contact, direction, experience, telegramName };

        valueRange.Values = new List<IList<object>> { objectList };

        var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadSheetId, range);

        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        _ = appendRequest.Execute();
    }

    public GoogleSheetsManager()
    {
        using (var stream = new FileStream("secret.json", FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(scopes);
        }
        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "DirectumCareerNightBot",
        });
    }
}

public static class GoogleSheets
{
    public static readonly string Interview = "Собеседования";
    public static readonly string Mature = "Взрослые";
    public static readonly string Trainee = "Практиканты";
}