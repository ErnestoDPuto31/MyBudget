using SQLite;

namespace MyBudget.Models
{
    public class Expense
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? Name { get; set; }
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
    }
}
