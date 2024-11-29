using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;


[ApiController]
[Route("api/ddt")]
public class GoogleSheetsController : ControllerBase
{
    private readonly GoogleSheetsService _sheetsService;

    public GoogleSheetsController(GoogleSheetsService sheetsService)
    {
        // Inject GoogleSheetsService via dependency injection
        _sheetsService = sheetsService;
    }

    [HttpPut("update-employee")]
public async Task<IActionResult> UpdateEmployeeAsync([FromBody] EmployeeData employee)
{
    if (employee == null || string.IsNullOrWhiteSpace(employee.Name))
    {
        return BadRequest(new { Message = "Invalid employee data." });
    }

    try
    {
        // Call the Google Sheets service to update the employee data
        bool success = await _sheetsService.UpdateEmployeeAsync(employee);

        if (success)
        {
            return Ok(new { Message = "Employee updated successfully." });
        }
        else
        {
            return NotFound(new { Message = "Employee not found." });
        }
    }
    catch (Exception ex)
    {
        // Log the exception (optional)
        Console.WriteLine(ex.Message);

        return StatusCode(500, new { Error = "An error occurred while updating the employee." });
    }
}


    
    [HttpPost("adduser")]
    public async Task<IActionResult> AddUserAsyncs([FromBody] Employee request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.userName))
        {
            return BadRequest("Invalid user data.");
        }

        try
        {
            var employeeData = new EmployeeData
            {
                Name = request.userName
            };

            // Call the Google Sheets service to add the user
            await _sheetsService.AddUserAsync(request.userName);

            return Ok(new { Message = "User added successfully." });
        }
        catch (Exception ex)
        {
            // Log the exception (optional)
            Console.WriteLine(ex.Message);

            return StatusCode(500, new { Error =ex.Message });
        }
    }
[HttpGet("employees-data")]
public async Task<IActionResult> GetEmployeesAsync([FromQuery] DateTime date)
{
    try
    {
        // Fetch employees for the specified date
        var employees = await _sheetsService.GetEmployeesAsync(date);

        // Return the filtered employees as JSON
        return Ok(employees);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching employees: {ex.Message}");
        return StatusCode(500, new { Message = "An error occurred while fetching employees." });
    }
}



[HttpPost("input-day-employee")]
public async Task<IActionResult> AddEmployee([FromBody] EmployeeData employee)
{
    try
    {
        // Validate input
        if (employee == null || string.IsNullOrWhiteSpace(employee.Name) || employee.Hours <= 0)
        {
            return BadRequest(new { Message = "Invalid employee data." });
        }

        // Fetch rows from the SaveDay sheet
        var saveDayRows = await _sheetsService.FetchRowsAsync("SaveDay!A2:I");

        // Check if the date exists in SaveDay and fetch current tips
        double cashTips = 0;
        double creditTips = 0;
        bool dateExistsInSaveDay = false;

        foreach (var row in saveDayRows)
        {
            Console.WriteLine($"Row: {string.Join(", ", row)}");

            if (row.Count > 0 &&
                DateTime.TryParseExact(row[0]?.ToString(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowDate))
            {
                Console.WriteLine($"Parsed Date: {rowDate}, Input Date: {employee.Date}");

                if (rowDate.Date == employee.Date.Date)
                {
                    dateExistsInSaveDay = true;
                    cashTips = row.Count > 1 && double.TryParse(row[1]?.ToString(), out var parsedCashTips) ? parsedCashTips : 0;
                    creditTips = row.Count > 2 && double.TryParse(row[2]?.ToString(), out var parsedCreditTips) ? parsedCreditTips : 0;

                    Console.WriteLine($"Matched Date: {rowDate}, CashTips: {cashTips}, CreditTips: {creditTips}");
                    break;
                }
            }
            else
            {
                Console.WriteLine($"Failed to parse or match date for row: {string.Join(", ", row)}");
            }
        }

        if (!dateExistsInSaveDay)
        {
            return BadRequest(new { Message = "The provided date does not exist in the SaveDay table." });
        }

        // Fetch rows from the UserInput sheet
        var userInputRows = await _sheetsService.FetchRowsAsync("UserInput!A2:C");
        int existingRowIndex = -1;

        // Check if the employee already exists for the given date
        for (int i = 0; i < userInputRows.Count; i++)
        {
            var row = userInputRows[i];
            if (row.Count > 1 &&
                DateTime.TryParseExact(row[0]?.ToString(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowDate) &&
                rowDate.Date == employee.Date.Date &&
                string.Equals(row[1]?.ToString(), employee.Name, StringComparison.OrdinalIgnoreCase))
            {
                existingRowIndex = i + 2; // Convert 0-based index to 1-based sheet row index
                break;
            }
        }

        if (existingRowIndex > 0)
        {
            // Update existing row
            var updatedRow = new List<object>
            {
                employee.Date.ToString("MM/dd/yyyy"), // Date
                employee.Name,                        // Employee Name
                employee.Hours                        // Hours Worked
            };
            string range = $"UserInput!A{existingRowIndex}:C{existingRowIndex}";
            await _sheetsService.UpdateSheetAsync(range, new List<IList<object>> { updatedRow });
        }
        else
        {
            // Add a new row
            var newRow = new List<object>
            {
                employee.Date.ToString("MM/dd/yyyy"), // Date
                employee.Name,                        // Employee Name
                employee.Hours                        // Hours Worked
            };
            await _sheetsService.AppendRowAsync("UserInput!A2:C", newRow);
        }

        // Invoke CalculateAndSaveTips for the given date with fetched tips
        var summaryData = new SummaryData
        {
            Date = employee.Date,
            CashTips = cashTips,
            CreditTips = creditTips
        };

        return await CalculateAndSaveTips(summaryData);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = ex.Message });
    }
}



   [HttpPost("calculate-and-save-tips")]
public async Task<IActionResult> CalculateAndSaveTips([FromBody] SummaryData request)
{
    try
    {
        // Fetch all existing rows from the SaveDay sheet
        var existingRows = await _sheetsService.FetchRowsAsync("SaveDay!A2:I");
        int existingRowIndex = -1;

        // Check if a row with the given date exists
    for (int i = 0; i < existingRows.Count; i++)
{
    var row = existingRows[i];
    if (row.Count > 0)
    {
        // Ensure the date parsing works correctly
        if (DateTime.TryParseExact(row[0]?.ToString(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowDate)
            && rowDate.Date == request.Date.Date)
        {
            existingRowIndex = i + 2; // Convert 0-based index to 1-based sheet row index
            break;
        }
    }
}


        // Fetch employee data for the given date
        var employees = await _sheetsService.GetEmployeesAsync(request.Date);

        // Calculate total employee hours
        double totalEmployeeHours = employees.Sum(e => e.Hours);

        // Avoid division by zero
        if (totalEmployeeHours <= 0)
        {
            return BadRequest(new { Message = "No employee hours available for the given date." });
        }

        // Calculate tips and performance metrics
        double totalTips = request.CashTips + request.CreditTips;
        double cashTipsPerHour = request.CashTips / totalEmployeeHours;
        double creditTipsPerHour = request.CreditTips / totalEmployeeHours;
        double totalTipsPerHour = totalTips / totalEmployeeHours;
        double completionTo50 = totalTipsPerHour < 50 ? 50 - totalTipsPerHour : 0;

        // Prepare the data to be saved
        var newRow = new List<object>
        {
            request.Date.ToString("MM/dd/yyyy"), // Date formatted for the sheet
            request.CashTips,
            request.CreditTips,
            totalTips,
            totalEmployeeHours,
            cashTipsPerHour,
            creditTipsPerHour,
            totalTipsPerHour,
            completionTo50
        };

        if (existingRowIndex > 0)
        {
            // Update the existing row
            string range = $"SaveDay!A{existingRowIndex}:I{existingRowIndex}";
            await _sheetsService.UpdateSheetAsync1(range, new List<IList<object>> { newRow });
        }
        else
        {
            // Append a new row
            await _sheetsService.AppendRowAsync("SaveDay!A2:I", newRow);
        }

        // Update EmployeeData sheet (reuse previous logic)
        var updatedRows = new List<IList<object>>();
        foreach (var employee in employees)
        {
            double updatedCashTips = cashTipsPerHour * employee.Hours;
            double updatedCreditTips = creditTipsPerHour * employee.Hours;

            updatedRows.Add(new List<object>
            {
                request.Date.ToString("MM/dd/yyyy"), // Date
                employee.Name,                      // Employee
                employee.Hours,                     // Hours
                updatedCashTips,                    // CashTips
                updatedCreditTips,                  // CreditTips
                updatedCashTips + updatedCreditTips // TotalTips
            });
        }

        await _sheetsService.UpdateSheetAsync("UserInput!A2:F", updatedRows);

        // Create the response model
        var response = new
        {
            Date = request.Date,
            CashTips = request.CashTips,
            CreditTips = request.CreditTips,
            TotalTips = totalTips,
            TotalEmployeesHours = totalEmployeeHours,
            CashTipsPerHour = cashTipsPerHour,
            CreditTipsPerHour = creditTipsPerHour,
            TotalTipsPerHour = totalTipsPerHour,
            CompletionTo50 = completionTo50
        };

        return Ok(response); // Return the response as JSON
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error calculating and saving tips: {ex.Message}");
        return StatusCode(500, new { Message = "An error occurred while processing the tips summary." });
    }
}



}
