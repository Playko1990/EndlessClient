using EndlessClient.Dialogs.Services;
using EndlessClient.HUD;
using EOLib.Domain.Character;
using EOLib.Extensions;
using EOLib.Graphics;
using EOLib.IO.Repositories;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moffat.EndlessOnline.SDK.Protocol.Net.Server;
using Optional;
using Optional.Unsafe;
using XNAControls;

namespace EndlessClient.Dialogs
{
    public class PlayerInfoDialog : BaseEODialog
    {
        private static readonly Rectangle _nativeIconDrawRect = new Rectangle(227, 258, 44, 21);

        private readonly IPubFileProvider _pubFileProvider;
        private readonly IPaperdollProvider _paperdollProvider;
        protected readonly bool _isMainCharacter;
        protected readonly float _dialogScale;

        private readonly Texture2D _characterIconSheet;
        private Option<Rectangle> _characterIconSourceRect;
        private Rectangle _iconDrawRect;

        private readonly IXNALabel _name,
            _home,
            _class,
            _partner,
            _title,
            _guild,
            _rank;

        public Character Character { get; }

        private Option<PaperdollData> _paperdollData;

        public PlayerInfoDialog(INativeGraphicsManager graphicsManager,
                                IEODialogButtonService eoDialogButtonService,
                                IPubFileProvider pubFileProvider,
                                IPaperdollProvider paperdollProvider,
                                Character character,
                                bool isMainCharacter)
            : base(graphicsManager, isInGame: true)
        {
            _pubFileProvider = pubFileProvider;
            _paperdollProvider = paperdollProvider;

            Character = character;
            _isMainCharacter = isMainCharacter;

            _dialogScale = Game.Window.AllowUserResizing ? HudUiMetrics.PaperdollScale : 1f;
            _iconDrawRect = HudUiMetrics.ScaleRectangle(_nativeIconDrawRect, _dialogScale);

            _characterIconSheet = GraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 32, true);
            _characterIconSourceRect = Option.None<Rectangle>();

            _name = CreateInfoLabel(228, 22);
            _home = CreateInfoLabel(228, 52);
            _class = CreateInfoLabel(228, 82);
            _partner = CreateInfoLabel(228, 112);
            _title = CreateInfoLabel(228, 142);
            _guild = CreateInfoLabel(228, 202);
            _rank = CreateInfoLabel(228, 232);

            var okOut = eoDialogButtonService.GetSmallDialogButtonOutSource(SmallButton.Ok);
            var okOver = eoDialogButtonService.GetSmallDialogButtonOverSource(SmallButton.Ok);
            var okButton = new XNAButton(
                eoDialogButtonService.SmallButtonSheet,
                HudUiMetrics.ScalePoint(276, 253, _dialogScale),
                okOut,
                okOver);
            if (_dialogScale > 1.01f)
            {
                var okPos = HudUiMetrics.ScalePoint(276, 253, _dialogScale);
                okButton.DrawArea = new Rectangle(
                    (int)okPos.X,
                    (int)okPos.Y,
                    HudUiMetrics.Scale(okOut.Width, _dialogScale),
                    HudUiMetrics.Scale(okOut.Height, _dialogScale));
                okButton.ClickArea = new Rectangle(0, 0, okButton.DrawArea.Width, okButton.DrawArea.Height);
            }
            okButton.OnMouseDown += (_, _) => Close(XNADialogResult.OK);
            okButton.Initialize();
            okButton.SetParentControl(this);

            _paperdollData = Option.None<PaperdollData>();
        }

        private XNALabel CreateInfoLabel(int x, int y)
        {
            var font = _dialogScale > 1.01f ? Constants.FontSize10 : Constants.FontSize08pt5;
            var label = new XNALabel(font)
            {
                DrawArea = new Rectangle(
                    HudUiMetrics.Scale(x, _dialogScale),
                    HudUiMetrics.Scale(y, _dialogScale),
                    1,
                    1),
                ForeColor = ColorConstants.LightGrayText
            };
            label.SetParentControl(this);
            return label;
        }

        public override void Initialize()
        {
            _name.Initialize();
            _home.Initialize();
            _class.Initialize();
            _partner.Initialize();
            _title.Initialize();
            _guild.Initialize();
            _rank.Initialize();

            base.Initialize();
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            _paperdollData = _paperdollData.FlatMap(paperdollData =>
                paperdollData.NoneWhen(d => _paperdollProvider.VisibleCharacterPaperdolls.ContainsKey(Character.ID) &&
                                           !_paperdollProvider.VisibleCharacterPaperdolls[Character.ID].Equals(d)));

            _paperdollData.MatchNone(() =>
            {
                if (_paperdollProvider.VisibleCharacterPaperdolls.ContainsKey(Character.ID))
                {
                    var paperdollData = _paperdollProvider.VisibleCharacterPaperdolls[Character.ID];
                    _paperdollData = Option.Some(paperdollData);
                    UpdateDisplayedData(paperdollData);
                }
            });

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            base.OnDrawControl(gameTime);

            _spriteBatch.Begin();

            _characterIconSourceRect.MatchSome(sourceRect =>
            {
                _spriteBatch.Draw(_characterIconSheet,
                    new Vector2(
                        DrawAreaWithParentOffset.X + _iconDrawRect.X + (_iconDrawRect.Width / 2) - (sourceRect.Width / 2),
                        DrawAreaWithParentOffset.Y + _iconDrawRect.Y + (_iconDrawRect.Height / 2) - (sourceRect.Height / 2)),
                    sourceRect,
                    Color.White);
            });

            _spriteBatch.End();
        }

        protected virtual void UpdateDisplayedData(PaperdollData paperdollData)
        {
            _name.Text = Capitalize(paperdollData.Name);
            _home.Text = Capitalize(paperdollData.Home);

            paperdollData.Class.SomeWhen(x => x != 0)
                .MatchSome(classId => _class.Text = Capitalize(_pubFileProvider.ECFFile[classId].Name));

            _partner.Text = Capitalize(paperdollData.Partner);
            _title.Text = Capitalize(paperdollData.Title);
            _guild.Text = Capitalize(paperdollData.Guild);
            _rank.Text = Capitalize(paperdollData.Rank);

            _characterIconSourceRect = Option.Some(GetOnlineIconSourceRectangle(paperdollData.Icon));
        }

        private static string Capitalize(string input) =>
            string.IsNullOrEmpty(input) ? string.Empty : char.ToUpper(input[0]) + input[1..].ToLower();

        private static Rectangle GetOnlineIconSourceRectangle(CharacterIcon icon)
        {
            var (x, y, width, height) = icon.ToChatIcon().GetChatIconRectangleBounds().ValueOrDefault();
            return new Rectangle(x, y, width, height);
        }
    }
}
