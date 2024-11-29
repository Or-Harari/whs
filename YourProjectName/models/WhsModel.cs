public class GoogleSheetsConfig
{
    public string ApplicationName { get; set; } = "My Web API";
    public string SpreadsheetId { get; set; } = "YOUR_SPREADSHEET_ID";
    public string Range { get; set; } = "Sheet1!A1:D10"; // Adjust as needed
}