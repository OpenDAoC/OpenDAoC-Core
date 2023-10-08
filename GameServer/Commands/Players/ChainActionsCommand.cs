using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands;

[Command("&chainactions", EPrivLevel.Player, "Chain multiple actions.", "/chainactions <create|save|clear>")]
public class ChainActionsCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "chainactions"))
            return;

        if (!Properties.ALLOW_CHAINED_ACTIONS)
        {
            client.Out.SendMessage("This command is not enabled on this server.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            return;
        }

        if (args.Length < 2)
        {
            DisplaySyntax(client);
            return;
        }

        GamePlayer player = client.Player;
        ChainedActions chainedActions = player.ChainedActions;

        switch (args[1])
        {
            case "create":
            {
                chainedActions.Create();
                break;
            }
            case "save":
            {
                chainedActions.Save();
                break;
            }
            case "clear":
            {
                chainedActions.Clear();
                break;
            }
            default:
            {
                DisplaySyntax(client);
                return;
            }
        }
    }
}

public class ChainedActions
{
    private const int MAX_ACTION_COUNT_PER_CHAIN = 5;

    private GamePlayer _player;
    private Dictionary<Skill, List<IChainedAction>> _actionChains = new();
    private CommandInputStep _commandStep;
    private List<IChainedAction> _actionChainBeingCreated;
    private int _actionChainBeingCreatedActionCount;

    public ChainedActions(GamePlayer player)
    {
        _player = player;
    }

    public void Create()
    {
        if (_commandStep != CommandInputStep.NONE)
        {
            _player.Out.SendMessage($"Finalize your current command before attempting to create a new chain.\nType '/chainactions clear' to abort.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            return;
        }

        _commandStep = CommandInputStep.CREATE;
        _player.Out.SendMessage($"Select the actions to add to your chain, in the desired execution order.\nType '/chainactions save' to save the chain.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
    }

    public void Save()
    {
        if (_commandStep != CommandInputStep.ADD)
        {
            _player.Out.SendMessage("You must start a new chain with '/chainactions create' before attempting to save one.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            return;
        }

        Skill firstSkill = _actionChainBeingCreated[0].Skill;
        _actionChains[firstSkill] = _actionChainBeingCreated;
        _player.Out.SendMessage($"You saved a new chain starting from {firstSkill.Name} and containing {_actionChainBeingCreatedActionCount} action{(_actionChainBeingCreatedActionCount > 1 ? "s" : "")}.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
        CleanUp();
    }

    private void CleanUp()
    {
        _commandStep = CommandInputStep.NONE;
        _actionChainBeingCreated = null;
        _actionChainBeingCreatedActionCount = 0;
    }

    public void Clear()
    {
        if (_commandStep == CommandInputStep.NONE)
        {
            _commandStep = CommandInputStep.CLEAR;
            _player.Out.SendMessage($"Select the first spell of an existing chain to clear it. No confirmation will be asked!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
        }
        else
        {
            _player.Out.SendMessage($"You cancelled your current '/chainactions' command.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            CleanUp();
        }
    }

    public bool Execute(Skill skill)
    {
        if (_actionChains.TryGetValue(skill, out List<IChainedAction> chain))
        {
            foreach (IChainedAction action in chain)
                action.Execute();

            return true;
        }

        return false;
    }

    private void Create(Spell spell, SpellLine spellLine)
    {
        CastingComponent.ChainedSpell chainedSpell = new(new CastingComponent.StartCastSpellRequest(spell, spellLine, null, null), _player);
        _actionChainBeingCreated = new() { chainedSpell };
    }

    private void Add(Spell spell, SpellLine spellLine)
    {
        CastingComponent.ChainedSpell chainedSpell = new(new CastingComponent.StartCastSpellRequest(spell, spellLine, null, null), _player);
        _actionChainBeingCreated.Add(chainedSpell);
    }

    public bool CheckCommandInput(Spell spell, SpellLine spellLine)
    {
        switch (_commandStep)
        {
            case CommandInputStep.CREATE:
            {
                Create(spell, spellLine);
                _player.Out.SendMessage($"{++_actionChainBeingCreatedActionCount}: {spell.Name}.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                _commandStep = CommandInputStep.ADD;
                return false;
            }
            case CommandInputStep.ADD:
            {
                Add(spell, spellLine);
                _player.Out.SendMessage($"{++_actionChainBeingCreatedActionCount}: {spell.Name}.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);

                if (_actionChainBeingCreatedActionCount == MAX_ACTION_COUNT_PER_CHAIN)
                {
                    _player.Out.SendMessage($"You cannot add any more action to this chain.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                    Save();
                }

                return false;
            }
            case CommandInputStep.CLEAR:
            {
                _commandStep = CommandInputStep.NONE;

                if (_actionChains.Remove(spell, out List<IChainedAction> chain))
                    _player.Out.SendMessage($"The chain starting with {chain[0].Skill.Name} has been removed.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                else
                    _player.Out.SendMessage($"{spell.Name} could not be resolved as the first spell of any chain.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);

                return false;
            }
            case CommandInputStep.NONE:
            default:
                return true;
        }
    }

    public enum CommandInputStep
    {
        NONE,
        CREATE,
        ADD,
        CLEAR
    }
}

public abstract class ChainedAction<T> : IChainedAction
{
    public virtual Skill Skill => null;
    protected T Handler { get; private set; }

    public ChainedAction(T handler)
    {
        Handler = handler;
    }

    public abstract void Execute();
}

public interface IChainedAction
{
    Skill Skill { get; }

    abstract void Execute();
}