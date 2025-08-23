using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services.interfaces
{
    public interface IPDFService
    {
        byte[] GeneratePdf();
        Task<(string, byte[])> SaveReceiptAsPdfAsync(ReceiptModel model, string subPath = "receipts");
    }
}
