namespace HostelManagementSystem.Models
{
    public class Invoice
    {
        public int InvoiceID { get; set; }
        public int LearnerID { get; set; }
        public string LearnerName { get; set; } // We'll join tables to get this
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string Description { get; set; }
        public bool IsPaid { get; set; }
    }
}
