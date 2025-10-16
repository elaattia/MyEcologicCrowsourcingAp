using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using MyEcologicCrowsourcingApp.DTOs;

namespace MyEcologicCrowsourcingApp.Services
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IOrganisationRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;

        public OrganisationService(IOrganisationRepository repository, IJwtService jwtService, IUserRepository userRepository)
        {
            _repository = repository;
            _jwtService = jwtService;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<OrganisationDto>> GetAllAsync()
        {
            var organisations = await _repository.GetAllAsync();
            return organisations.Select(o => new OrganisationDto
            {
                OrganisationId = o.OrganisationId,
                Nom = o.Nom,
                NbrVolontaires = o.NbrVolontaires
            });
        }

        public async Task<OrganisationDetailDto?> GetByIdAsync(Guid id)
        {
            var organisation = await _repository.GetByIdAsync(id);
            if (organisation == null) return null;

            return new OrganisationDetailDto
            {
                OrganisationId = organisation.OrganisationId,
                Nom = organisation.Nom,
                NbrVolontaires = organisation.NbrVolontaires,
                RepresentantId = organisation.RepresentantId 
            };
        }

        public async Task<OrganisationCreationResult> CreateWithRepresentativeAsync(Organisation organisation, User representant)
        {
            // 1. Générer les IDs
            representant.UserId = Guid.NewGuid();
            organisation.OrganisationId = Guid.NewGuid();
            
            // 2. Configurer le représentant
            representant.Role = UserRole.Representant;
            representant.Password = BCrypt.Net.BCrypt.HashPassword(representant.Password);
            representant.OrganisationId = null;
            
            // 3. Configurer l'organisation
            organisation.RepresentantId = representant.UserId;
            organisation.Vehicules ??= new List<Vehicule>();
            organisation.Depots ??= new List<Depot>();
            organisation.Itineraires ??= new List<Itineraire>();

            // 4. Insérer le représentant
            await _userRepository.AddAsync(representant);

            // 5. Insérer l'organisation
            await _repository.AddAsync(organisation);

            // 6. Mettre à jour le représentant
            representant.OrganisationId = organisation.OrganisationId;
            await _userRepository.UpdateAsync(representant);

            // 7. Générer le token
            string jwt = _jwtService.GenerateToken(representant);

            return new OrganisationCreationResult
            {
                Organisation = organisation,
                Token = jwt
            };
        }

        public async Task<OrganisationDto?> UpdateAsync(Guid id, Organisation organisation)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Nom = organisation.Nom;
            existing.NbrVolontaires = organisation.NbrVolontaires;

            await _repository.UpdateAsync(existing);
            
            return new OrganisationDto
            {
                OrganisationId = existing.OrganisationId,
                Nom = existing.Nom,
                NbrVolontaires = existing.NbrVolontaires
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            await _repository.DeleteAsync(id);
            return true;
        }
    }
}