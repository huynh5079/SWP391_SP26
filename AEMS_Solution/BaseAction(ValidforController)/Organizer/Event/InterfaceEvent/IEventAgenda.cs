using AEMS_Solution.Models.Event.EventAgenda;

namespace AEMS_Solution.BaseAction_ValidforController_.Organizer.Event.InterfaceEvent
{
	public interface IEventAgendaAction
	{
		string EnsureOrganizerId(string? organizerId);
		Task<DataAccess.Entities.EventAgenda> EnsureAgendaOwnershipAsync(string organizerId, string agendaId);
		void EnsureValidAgenda(EditAgendaViewModel model);
	}
}
