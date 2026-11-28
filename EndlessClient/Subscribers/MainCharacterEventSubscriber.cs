using AutomaticTypeMapper;

using EndlessClient.Audio;
using EndlessClient.HUD;
using EndlessClient.Rendering.Character;

using EOLib.Domain.Chat;
using EOLib.Domain.Notifiers;
using EOLib.IO.Extensions;
using EOLib.IO.Repositories;
using EOLib.Localization;

namespace EndlessClient.Subscribers
{
    [MappedType(BaseType = typeof(IMainCharacterEventNotifier))]
    public class MainCharacterEventSubscriber : IMainCharacterEventNotifier
    {
        private readonly IStatusLabelSetter _statusLabelSetter;
        private readonly IChatRepository _chatRepository;
        private readonly ILocalizedStringFinder _localizedStringFinder;
        private readonly IPubFileProvider _pubFileProvider;
        private readonly ICharacterRendererProvider _characterRendererProvider;
        private readonly ISfxPlayer _sfxPlayer;

        public MainCharacterEventSubscriber(IStatusLabelSetter statusLabelSetter,
                                            IChatRepository chatRepository,
                                            ILocalizedStringFinder localizedStringFinder,
                                            IPubFileProvider pubFileProvider,
                                            ICharacterRendererProvider characterRendererProvider,
                                            ISfxPlayer sfxPlayer)
        {
            _statusLabelSetter = statusLabelSetter;
            _chatRepository = chatRepository;
            _localizedStringFinder = localizedStringFinder;
            _pubFileProvider = pubFileProvider;
            _characterRendererProvider = characterRendererProvider;
            _sfxPlayer = sfxPlayer;
        }

        public void NotifyGainedExp(int expDifference)
        {
            _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_INFORMATION,
                                              EOResourceID.STATUS_LABEL_YOU_GAINED_EXP,
                $" {expDifference} EXP");

            var youGained = _localizedStringFinder.GetString(EOResourceID.STATUS_LABEL_YOU_GAINED_EXP);
            var message = $"{youGained} {expDifference} EXP";

            var chatData = new ChatData(ChatTab.System, string.Empty, message, ChatIcon.Star);
            _chatRepository.AllChat[ChatTab.System].Add(chatData);
        }

        public void NotifyTakeDamage(int damageTaken, int playerPercentHealth, bool isHeal)
        {
            if (isHeal && damageTaken == 0) return;

            _characterRendererProvider.MainCharacterRenderer.MatchSome(r => r.ShowDamageCounter(damageTaken, playerPercentHealth, isHeal));
        }

        public void TakeItemFromMap(int id, int amountTaken)
        {
            var name = _pubFileProvider.EIFFile.GetItemOrUnknown(id).Name;

            var chatMessage = $"{_localizedStringFinder.GetString(EOResourceID.STATUS_LABEL_ITEM_PICKUP_YOU_PICKED_UP)} {amountTaken} {name}";
            _chatRepository.AllChat[ChatTab.System].Add(new ChatData(ChatTab.System, string.Empty, chatMessage, ChatIcon.UpArrow));

            _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_INFORMATION, EOResourceID.STATUS_LABEL_ITEM_PICKUP_YOU_PICKED_UP,
                $" {amountTaken} {name}");
        }

        public void DropItem(int id, int amountDropped)
        {
            var name = _pubFileProvider.EIFFile.GetItemOrUnknown(id).Name;

            var chatMessage = $"{_localizedStringFinder.GetString(EOResourceID.STATUS_LABEL_ITEM_DROP_YOU_DROPPED)} {amountDropped} {name}";
            _chatRepository.AllChat[ChatTab.System].Add(new ChatData(ChatTab.System, string.Empty, chatMessage, ChatIcon.DownArrow));

            _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_INFORMATION, EOResourceID.STATUS_LABEL_ITEM_DROP_YOU_DROPPED,
                $" {amountDropped} {name}");
        }

        public void JunkItem(int id, int amountRemoved)
        {
            var name = _pubFileProvider.EIFFile.GetItemOrUnknown(id).Name;

            var chatMessage = $"{_localizedStringFinder.GetString(EOResourceID.STATUS_LABEL_ITEM_JUNK_YOU_JUNKED)} {amountRemoved} {name}";
            _chatRepository.AllChat[ChatTab.System].Add(new ChatData(ChatTab.System, string.Empty, chatMessage, ChatIcon.DownArrow));

            _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_INFORMATION, EOResourceID.STATUS_LABEL_ITEM_JUNK_YOU_JUNKED,
                $" {amountRemoved} {name}");
        }

        public void NotifyFrozen() => _sfxPlayer.PlaySfx(SoundEffectID.PlayerFrozen);

        public void NotifySit() => _sfxPlayer.PlaySfx(SoundEffectID.Sit);
    }
}
