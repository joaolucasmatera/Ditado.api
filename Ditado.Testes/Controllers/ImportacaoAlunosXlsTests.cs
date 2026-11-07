using System.Data;
using System.Text;
using ExcelDataReader;
using NPOI.HSSF.UserModel;
using Xunit;

namespace Ditado.Testes.Controllers;

public sealed class ImportacaoAlunosXlsTests
{
    [Fact]
    public void FixtureXlsBiff_DeveSerLidoPeloExcelDataReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Alunos");
        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("Nome");
        header.CreateCell(1).SetCellValue("Número da chamada");
        header.CreateCell(2).SetCellValue("Matrícula");
        header.CreateCell(3).SetCellValue("Turma");
        var row = sheet.CreateRow(1);
        row.CreateCell(0).SetCellValue("Aluno Fixture");
        row.CreateCell(1).SetCellValue(7);
        row.CreateCell(2).SetCellValue("MAT-007");
        row.CreateCell(3).SetCellValue("7A");
        using var output = new MemoryStream();
        workbook.Write(output, leaveOpen: true);
        output.Position = 0;
        using var reader = ExcelReaderFactory.CreateReader(output);
        var data = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true } });
        Assert.Single(data.Tables);
        Assert.Equal("Aluno Fixture", data.Tables[0].Rows[0]["Nome"]);
        Assert.Equal("7", data.Tables[0].Rows[0]["Número da chamada"]?.ToString());
    }
}
