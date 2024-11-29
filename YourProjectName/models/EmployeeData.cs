public class EmployeeData
{
    public DateTime Date { get; set; } // Represents the date
    public string Name { get; set; } // Employee's name
    public double Hours { get; set; } // Total working hours
    public double CashTips { get; set; } // Tips received in cash
    public double CreditTips { get; set; } // Tips received via credit
    public double TotalTips => CashTips + CreditTips; // Computed property for total tips
}
