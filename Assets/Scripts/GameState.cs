// Nicolas Robert [Nrx]

public interface IGameState
{
	void Enter (Game game);
	void Execute (Game game);
	void Exit (Game game);
}
