namespace MyEcologicCrowsourcingApp.Models
{
    public enum TypeVehicule
    {
        Camion,
        Camionette,
        Voiture,
        VeloCargo
    }

    public class Vehicule
    {
        public Guid Id { get; set; }
        public TypeVehicule Type { get; set; }
        public double CapaciteMax { get; set; }
        public double VitesseMoyenne { get; set; }
        public double CarburantConsommation { get; set; }
    }
}
