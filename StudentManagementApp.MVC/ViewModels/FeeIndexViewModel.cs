using System;

namespace StudentManagementApp.MVC.ViewModels
{
    public class FeeIndexViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}