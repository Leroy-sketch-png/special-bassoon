using MoePortal.Core.Domain.Entities;
using System.Xml.Linq;

namespace MoePortal.Infrastructure.Services;

public interface ICpfPayoutService
{
    Task GenerateIso20022PayoutFileAsync(CitizenRecord citizen, decimal balance, CancellationToken ct = default);
}

public class CpfPayoutService : ICpfPayoutService
{
    private readonly string _outputDirectory = "CpfPayouts";

    public CpfPayoutService()
    {
        if (!Directory.Exists(_outputDirectory))
            Directory.CreateDirectory(_outputDirectory);
    }

    public async Task GenerateIso20022PayoutFileAsync(CitizenRecord citizen, decimal balance, CancellationToken ct = default)
    {
        if (balance <= 0) return;

        var messageId = $"MOE-CPF-{DateTime.UtcNow:yyyyMMddHHmmss}-{citizen.Nric}";
        
        // Simplified ISO 20022 pain.001.001.03
        var xml = new XElement("Document",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute("xmlns", "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03"),
            new XElement("CstmrCdtTrfInitn",
                new XElement("GrpHdr",
                    new XElement("MsgId", messageId),
                    new XElement("CreDtTm", DateTime.UtcNow.ToString("o")),
                    new XElement("NbOfTxs", 1),
                    new XElement("CtrlSum", balance),
                    new XElement("InitgPty",
                        new XElement("Nm", "Ministry of Education")
                    )
                ),
                new XElement("PmtInf",
                    new XElement("PmtInfId", $"PMT-{citizen.Nric}"),
                    new XElement("PmtMtd", "TRF"),
                    new XElement("ReqdExctnDt", DateTime.UtcNow.Date.ToString("yyyy-MM-dd")),
                    new XElement("Dbtr",
                        new XElement("Nm", "MOE Edusave")
                    ),
                    new XElement("DbtrAcct",
                        new XElement("Id",
                            new XElement("Othr",
                                new XElement("Id", "MOE-EDU-9999")
                            )
                        )
                    ),
                    new XElement("DbtrAgt",
                        new XElement("FinInstnId",
                            new XElement("BIC", "MASBSGSG")
                        )
                    ),
                    new XElement("CdtTrfTxInf",
                        new XElement("PmtId",
                            new XElement("EndToEndId", messageId)
                        ),
                        new XElement("Amt",
                            new XElement("InstdAmt", new XAttribute("Ccy", "SGD"), balance)
                        ),
                        new XElement("CdtrAgt",
                            new XElement("FinInstnId",
                                new XElement("BIC", "CPFBOARD")
                            )
                        ),
                        new XElement("Cdtr",
                            new XElement("Nm", citizen.FullName)
                        ),
                        new XElement("CdtrAcct",
                            new XElement("Id",
                                new XElement("Othr",
                                    new XElement("Id", citizen.Nric)
                                )
                            )
                        )
                    )
                )
            )
        );

        var filePath = Path.Combine(_outputDirectory, $"{messageId}.xml");
        await File.WriteAllTextAsync(filePath, xml.ToString(), ct);
    }
}
