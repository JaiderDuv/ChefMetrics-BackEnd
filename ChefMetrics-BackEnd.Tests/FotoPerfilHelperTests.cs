using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Tests;

public sealed class FotoPerfilHelperTests
{
    [Theory]
    [InlineData(null)][InlineData("")][InlineData(" ")]
    public void Vacio_elimina_foto(string? input)
    {
        Assert.True(FotoPerfilHelper.TryNormalizar(input, out var normalized, out var error));
        Assert.Null(normalized); Assert.Null(error);
    }
    [Theory]
    [InlineData("png")][InlineData("jpeg")][InlineData("jpg")][InlineData("webp")][InlineData("gif")][InlineData("bmp")]
    public void Acepta_formato_permitido_y_recorta_espacios(string mime)
    {
        var value = $"data:image/{mime};base64,AQID";
        Assert.True(FotoPerfilHelper.TryNormalizar(" " + value + " ", out var normalized, out var error));
        Assert.Equal(value, normalized); Assert.Null(error);
    }
    [Theory]
    [InlineData("https://example.com/a.png")][InlineData("data:,AQID")][InlineData("data:image/png")]
    [InlineData("data:image/svg+xml;base64,AQID")][InlineData("data:image/png;text,AQID")]
    [InlineData("data:image/png;base64,!")][InlineData("data:image/png;base64,")]
    public void Rechaza_formato_o_base64_invalido(string value)
    {
        Assert.False(FotoPerfilHelper.TryNormalizar(value, out var normalized, out var error));
        Assert.Null(normalized); Assert.NotEmpty(error!);
    }
    [Theory]
    [InlineData(2097152, true)][InlineData(2097153, false)][InlineData(3000001, false)]
    public void Respeta_limite_de_dos_megabytes(int bytes, bool accepted)
    {
        var value = "data:image/png;base64," + Convert.ToBase64String(new byte[bytes]);
        Assert.Equal(accepted, FotoPerfilHelper.TryNormalizar(value, out var normalized, out var error));
        if (accepted) { Assert.Equal(value, normalized); Assert.Null(error); }
        else { Assert.Null(normalized); Assert.Contains("2 MB", error); }
    }
}
