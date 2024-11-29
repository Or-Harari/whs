using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;


public class GoogleSheetsService
{
    private readonly SheetsService _service;
    private readonly string _spreadsheetId;

    public GoogleSheetsService(SheetsService service, string spreadsheetId)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _spreadsheetId = spreadsheetId ?? throw new ArgumentNullException(nameof(spreadsheetId));
    }

    private bool TryParseHours(string? input, out double hours)
    {
        hours = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (TimeSpan.TryParse(input, out var timeSpan))
        {
            hours = timeSpan.TotalHours;
            return true;
        }

        return double.TryParse(input, out hours);
    }

public async Task AddUserAsync(string userName)
{
    if (string.IsNullOrWhiteSpace(userName))
        throw new ArgumentException("User name cannot be null or empty.", nameof(userName));

    // Fetch the existing users
    var existingUsers = await GetExistingUsersAsync();

    // Check if the user already exists
    if (existingUsers.Contains(userName, StringComparer.OrdinalIgnoreCase))
        throw new InvalidOperationException("User already exists.");

    // Add the new user
    var newUser = new List<object> { userName };
    await AppendRowAsync("whs!A4", newUser);
}
private async Task<List<string>> GetExistingUsersAsync()
{
    // Fetch data from the sheet
    var valueRange = await FetchDataAsync("whs!A4:A");

    // Check if there are any values
    if (valueRange.Values == null || valueRange.Values.Count == 0)
        return new List<string>(); // Return an empty list if no data is found

    // Convert rows to a list of strings (assuming single-column range like "whs!A4:A")
    return valueRange.Values
        .Select(row => row.FirstOrDefault()?.ToString() ?? string.Empty) // Get the first cell of each row
        .Where(value => !string.IsNullOrWhiteSpace(value)) // Exclude empty or whitespace rows
        .ToList();
}

public async Task AddEmployeeAsync(EmployeeInput employee)
{
    var newRow = new List<object>
    {
        employee.Date.ToString("MM/dd/yyyy"), // Date
        employee.Name,                        // Employee Name
        employee.Hours                        // Hours Worked
    };

    await AppendRowAsync("UserInput!A2:C", newRow);
}


public async Task<bool> UpdateEmployeeAsync(EmployeeData updatedEmployee)
{
    if (updatedEmployee == null || string.IsNullOrWhiteSpace(updatedEmployee.Name))
        throw new ArgumentNullException(nameof(updatedEmployee), "Employee data or name cannot be null or empty.");

    // Adjust the range to cover the 'Name' column (B) and data rows
    string range = "whs!B4:E"; // Start from B4 since Name is in column B
    var response = await FetchDataAsync(range);

    if (response.Values == null)
    {
        Console.WriteLine("No data found in the sheet.");
        return false;
    }

    // Locate the row for the employee based on the Name in column B
    int rowIndex = -1;
    for (int i = 0; i < response.Values.Count; i++)
    {
        var row = response.Values[i];
        if (row.Count > 0 && string.Equals(row[0]?.ToString()?.Trim(), updatedEmployee.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            rowIndex = i;
            break;
        }
    }

    if (rowIndex == -1)
    {
        Console.WriteLine($"Employee '{updatedEmployee.Name}' not found.");
        return false;
    }

    // Calculate the row number in the sheet (adjust based on your starting row)
    int sheetRow = rowIndex + 4; // Start at row 4 in the sheet

    // Prepare the updated row data (keeping the Name in column B)
    var updatedRow = new List<object>
    {
        updatedEmployee.Name,  // Column B
        $"{(int)updatedEmployee.Hours}:{(int)((updatedEmployee.Hours - (int)updatedEmployee.Hours) * 60)}:00", // Hours
        updatedEmployee.CashTips,  // Cash Tips
        updatedEmployee.CreditTips // Credit Tips
    };

    var valueRange = new ValueRange { Values = new List<IList<object>> { updatedRow } };

    // Define the exact range for the row to update (shift to column B)
    string updateRange = $"whs!B{sheetRow}:E{sheetRow}";

    // Update the row in Google Sheets
    var updateRequest = _service.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange);
    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

    await updateRequest.ExecuteAsync();
    return true;
}
  public async Task<List<LoginRequest>> GetUsersAsync()
{
    var range = "whs!A2:B3"; // Explicitly fetch only rows 2 and 3
    var response = await FetchDataAsync(range);

    var users = new List<LoginRequest>();
    if (response?.Values != null)
    {
        foreach (var row in response.Values)
        {
            var userName = row.Count > 0 ? row[0]?.ToString() : null;
            var password = row.Count > 1 ? row[1]?.ToString() : null;

            if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
            {
                users.Add(new LoginRequest(userName, password));
            }
        }
    }

    return users;
}

  public async Task<List<EmployeeData>> GetEmployeesAsync(DateTime date)
{
    var range = "UserInput!A2:F"; // Adjust the range as needed
    var response = await FetchDataAsync(range);

    var employees = new List<EmployeeData>();

    if (response?.Values != null)
    {
        foreach (var row in response.Values)
        {
            // Ensure the row has at least the required columns
            if (row.Count < 3) continue;

            // Parse the date explicitly using the known format
            var rowDate = DateTime.TryParseExact(
                row[0]?.ToString(),
                "MM/dd/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate
            ) ? parsedDate : DateTime.MinValue;

            // Only process rows for the given date
            if (rowDate.Date == date.Date)
            {
                var employee = new EmployeeData
                {
                    Date = rowDate,
                    Name = row.Count > 1 ? row[1]?.ToString() : string.Empty,
                    Hours = row.Count > 2 && double.TryParse(row[2]?.ToString(), out var hours) ? hours : 0,
                    CashTips = row.Count > 3 && double.TryParse(row[3]?.ToString(), out var cashTips) ? cashTips : 0,
                    CreditTips = row.Count > 4 && double.TryParse(row[4]?.ToString(), out var creditTips) ? creditTips : 0
                };

                employees.Add(employee);
            }
        }
    }

    return employees;
}
    public async Task AppendRowAsync(string range, IList<object> row)
    {
        var valueRange = new ValueRange { Values = new List<IList<object>> { row } };

        var appendRequest = _service.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        await appendRequest.ExecuteAsync();
    }

    private async Task<ValueRange> FetchDataAsync(string range)
    {
        var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);
        return await request.ExecuteAsync();
    }
    public async Task UpdateSheetAsync(string range, IList<IList<object>> updatedRows)
{
    try
    {
        // Prepare the request body with updated rows
        var requestBody = new ValueRange
        {
            Values = updatedRows
        };

        // Create the update request
        var updateRequest = _service.Spreadsheets.Values.Update(requestBody, _spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        // Execute the update
        await updateRequest.ExecuteAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating sheet: {ex.Message}");
        throw new InvalidOperationException("An error occurred while updating the sheet.", ex);
    }
}
public async Task<IList<IList<object>>> FetchRowsAsync(string range)
{
    var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);
    var response = await request.ExecuteAsync();
    return response.Values ?? new List<IList<object>>();
}
public async Task UpdateSheetAsync1(string range, IList<IList<object>> updatedRows)
{
    var requestBody = new ValueRange { Values = updatedRows };
    var updateRequest = _service.Spreadsheets.Values.Update(requestBody, _spreadsheetId, range);
    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
    await updateRequest.ExecuteAsync();
}


}
