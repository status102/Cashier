using Cashier.Commons;
using ImGuiNET;
using System.Numerics;

namespace Cashier.Windows
{
    public unsafe sealed class Main : IWindow
    {
        private readonly Cashier _cashier;
        private readonly static Vector2 Window_Size = new(720, 640);
        private bool _visible;
        private readonly TeamHelper _teamHelper = new ();
        public Main(Cashier cashier)
        {
            _cashier = cashier;
        }

        public void Dispose()
        {
        }

        public void Show()
        {
            _visible = !_visible;
        }

        public void Draw()
        {
            if (!_visible) {
                return;
            }
            ImGui.SetNextWindowSize(Window_Size, ImGuiCond.Once);
            if (ImGui.Begin($"主窗口##{_cashier.Name}Main", ref _visible)) {
                if (ImGui.Button("测试按钮")) {
                    
                    _teamHelper.UpdateTeamList();
                }
                _teamHelper.TeamList.ForEach(member =>
                {
                    ImGui.Text(member.FirstName);
                });
                ImGui.End();
            }
        }
        /*
        private void UpdatePartyList()
        {
            var groupManager = GroupManager.Instance();
            if (groupManager->MemberCount > 0) {
                this.AddMembersFromGroupManager(groupManager);
            } else {
                var cwProxy = InfoProxyCrossRealm.Instance();
                if (cwProxy->IsInCrossRealmParty != 0) {
                    var localIndex = cwProxy->LocalPlayerGroupIndex;
                    this.AddMembersFromCRGroup(cwProxy->CrossRealmGroupArraySpan[localIndex]，true);
                    for (var i = 0; i < cwProxy->CrossRealmGroupArraySpan.Length; i++) {
                        if (i == localIndex) {
                            continue;
                        }
                        this.AddMembersFromCRGroup(cwProxy->CrossRealmGroupArraySpan[i]);
                    }
                }
            }
            // Add self if not in party
            if (this.TeamList.Count == 0 && svc.clientstate.LocalPlayer != null) { var selfName = Service.clientstate.LocalPlayer.Name.TextValue;
                var selfworldId = Service.clientstate.LocalPlayer.Homeworld.Id;
                var selfJobId = Service.clientstate.LocalPlayer.class3ob.Id;
                this.AddTeamMember(selfName，(ushort)selfworldId，self3obId，true);
            }
        }*/
    }
}
