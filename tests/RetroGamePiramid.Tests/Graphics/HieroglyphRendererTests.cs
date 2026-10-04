using Microsoft.Xna.Framework;
using RetroGamePiramid.Graphics;
using Xunit;

namespace RetroGamePiramid.Tests.Graphics;

public class HieroglyphRendererTests
{
    [Fact]
    public void GenerateMuralPixels_ReturnsArrayWithExactDimensions()
    {
        const int width = HieroglyphRenderer.MURAL_WIDTH;
        const int height = HieroglyphRenderer.MURAL_HEIGHT;

        Color[] pixels = HieroglyphRenderer.GenerateMuralPixels(width, height);

        Assert.NotNull(pixels);
        Assert.Equal(width * height, pixels.Length);
    }

    [Fact]
    public void GenerateMuralPixels_ContainsLowContrastSubtlePalette()
    {
        // El requerimiento exige que los grabados sean "poco visibles con una tonada muy suave, que apenas se puedan distinguir"
        // El fondo de la cámara es RGB(18, 12, 22). Los píxeles dibujados deben tener brillo suave (canales <= 90)
        Color[] pixels = HieroglyphRenderer.GenerateMuralPixels(HieroglyphRenderer.MURAL_WIDTH, HieroglyphRenderer.MURAL_HEIGHT);

        int nonTransparentCount = 0;
        foreach (Color c in pixels)
        {
            if (c.A > 0)
            {
                nonTransparentCount++;
                // Ningún píxel del mural de fondo debe tener brillo deslumbrante (debe ser sutil bajorrelieve)
                Assert.True(c.R <= 90, $"El canal R ({c.R}) supera el umbral suave");
                Assert.True(c.G <= 70, $"El canal G ({c.G}) supera el umbral suave");
                Assert.True(c.B <= 55, $"El canal B ({c.B}) supera el umbral suave");
            }
        }

        Assert.True(nonTransparentCount > 500, "El mural debe contener al menos 500 píxeles de grabados tallados");
    }

    [Fact]
    public void GenerateMuralPixels_ContainsPuzzle1FormulaSequence()
    {
        // Verifica que la secuencia de la fórmula del Puzle 1 esté presente:
        // [Cofre 1 en x~105] + [Ankh en x~118] + [Cofre 2 en x~130] + [Ankh en x~143] + [Llave en x~154]
        // + [Puertas en x~175..198] + [Cofre Radiante en x~220]
        const int width = HieroglyphRenderer.MURAL_WIDTH;
        Color[] pixels = HieroglyphRenderer.GenerateMuralPixels(width, HieroglyphRenderer.MURAL_HEIGHT);

        // 1. Cofre 1 en (105, 22)
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 105]);

        // 2. Ankh 1 en (118, 22)
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 118]);

        // 3. Cofre 2 en (130, 22)
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 130]);

        // 4. Ankh 2 en (143, 22)
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 143]);

        // 5. Llave en (154, 22)
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 154]);

        // 6. Flecha en (165, 23)
        Assert.NotEqual(Color.Transparent, pixels[23 * width + 165]);

        // 7. Tres Puertas:
        // Indicadores superiores | | | en y = 13
        Assert.NotEqual(Color.Transparent, pixels[13 * width + 177]); // Marca 1
        Assert.NotEqual(Color.Transparent, pixels[13 * width + 186]); // Marca 2
        Assert.NotEqual(Color.Transparent, pixels[13 * width + 195]); // Marca 3

        // Puerta 1 en x = 175, y = 20
        Assert.NotEqual(Color.Transparent, pixels[20 * width + 175]);
        // Puerta 2 en x = 184, y = 20
        Assert.NotEqual(Color.Transparent, pixels[20 * width + 184]);
        // Puerta 3 en x = 193, y = 20
        Assert.NotEqual(Color.Transparent, pixels[20 * width + 193]);

        // 8. Cofre Radiante Extra en (220, 22)
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 220]);
        // Rayo solar del cofre radiante en (220, 13)
        Assert.NotEqual(Color.Transparent, pixels[13 * width + 220]);
    }

    [Fact]
    public void GenerateMuralPixels_ContainsLeftSectionEgyptianArt()
    {
        // La sección izquierda contiene el Ojo de Horus (x~32, y~22) y el Cartucho (x~60, y~20)
        const int width = HieroglyphRenderer.MURAL_WIDTH;
        Color[] pixels = HieroglyphRenderer.GenerateMuralPixels(width, HieroglyphRenderer.MURAL_HEIGHT);

        // Ojo de Horus
        Assert.NotEqual(Color.Transparent, pixels[22 * width + 32]);

        // Cartucho del Faraón
        Assert.NotEqual(Color.Transparent, pixels[20 * width + 60]);
    }
}
