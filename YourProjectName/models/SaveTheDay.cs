public class SummaryData
{
    public DateTime Date { get; set; } // Represents the date
    public double CashTips { get; set; } // Total cash tips
    public double CreditTips { get; set; } // Total credit tips
    public double TotalTips => CashTips + CreditTips; // Computed property for total tips
    public double TotalEmployeesHours { get; set; } // Sum of all employees' hours
    public double CashTipsPerHour => TotalEmployeesHours > 0 ? CashTips / TotalEmployeesHours : 0; // Cash tips per hour
    public double CreditTipsPerHour => TotalEmployeesHours > 0 ? CreditTips / TotalEmployeesHours : 0; // Credit tips per hour
    public double TotalTipsPerHour => TotalEmployeesHours > 0 ? TotalTips / TotalEmployeesHours : 0; // Total tips per hour
    public double CompletionTo50 => TotalTips / 50 * 100; // Percentage completion toward a target of 50
}
