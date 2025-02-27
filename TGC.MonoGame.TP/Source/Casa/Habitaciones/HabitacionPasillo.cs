using System;
using Microsoft.Xna.Framework;
using PistonDerby.Elementos;

namespace PistonDerby;
public class HabitacionPasillo : IHabitacion{
    public const int ANCHO = 8;
    public const int LARGO = 4;
    public HabitacionPasillo(float posicionX, float posicionZ):base(ANCHO,LARGO,new Vector3(posicionX,0f,posicionZ)){  
        Piso.ConTextura(PistonDerby.GameContent.T_PisoAlfombrado, ANCHO, LARGO*0.5f);

        var posicionInicial = new Vector3(posicionX,0f,posicionZ);

       Amueblar();

    }
     private void Amueblar(){
        var carpintero = new ElementoBuilder(this.PuntoInicio());
    
        carpintero.Modelo(PistonDerby.GameContent.M_Plantis)
            .ConPosicion(1, 1)
            .ConPBRempaquetado(PistonDerby.GameContent.T_Plantis_RoughnessMetallicOpacityMap, PistonDerby.GameContent.T_Plantis_BaseColorMap, PistonDerby.GameContent.T_Plantis_NormalMap)
            .ConCaja(65f,200f,65f) // Ancho (x), Alto (y), Profundidad (z)
            .ConCorrimientoCaja(0,-10,0)
            .ConEscala(5f);
        AddElemento(carpintero.BuildMueble());

            carpintero.ConPosicion(LARGO-1, 1);
        AddElemento(carpintero.BuildMueble());
            carpintero.ConPosicion(LARGO-1, 3);
        AddElemento(carpintero.BuildMueble());
            carpintero.ConPosicion(LARGO-1, 5);
        AddElemento(carpintero.BuildMueble());
            carpintero.ConPosicion(1, 3);
        AddElemento(carpintero.BuildMueble());
            carpintero.ConPosicion(1, 5);
        AddElemento(carpintero.BuildMueble());

    }

}
