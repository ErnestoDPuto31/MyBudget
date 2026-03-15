using SQLite;

namespace MyBudget.Models
{
    public class BudgetSettings
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; 
        public BudgetPeriod Period { get; set; }
        public decimal Amount { get; set; }
    }
}
