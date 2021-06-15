// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Maze data
	private const int mazePathCountWidth = 7;
	private const int mazePathCountHeight = 10;
	private byte [] maze = new byte [(mazePathCountWidth + 1) * (mazePathCountHeight + 1)];
	private int [] mazeDirectionOffset = {-1, -mazePathCountWidth - 1, 1, mazePathCountWidth + 1};
	private enum MazeFlags {
		DIRECTION_MASK = 3,
		DIRECTION_INVALID = 1 << 2,
		BLOCK = 1 << 3,
		PATH = 1 << 4,
		DOOR_OPENED_TOP = 1 << 5,
		DOOR_OPENED_LEFT = 1 << 6,
	};
	private byte [,] mazeSymbols = {
		{ 0, 28, 34, 32,  32,  24, 32, 34,  28,  0, 0},
		{ 0, 62,  2,  4,   8,  16, 32, 34,  28,  0, 0},
		{ 0, 28,  8,  8,   8,   8,  8, 12,   8,  0, 0},
		{48, 72, 72, 72,  48,   6,  9, 13,   1,  6, 0},
		{62, 42, 42, 62, 119, 127, 73, 73, 127, 62, 0},
	};
	private byte [] mazeTitle = {
		 0,  0,   0,  0,  0,
		62,  2,   2, 30,  2,  2,  2, 0, 0,
		 2,  2,   2,  2,  2,  2, 62, 0, 0,
		28, 34,  34, 62, 34, 34, 34, 0, 0,
		34, 54,  42, 34, 34, 34, 34, 0, 0,
		28,  8,   8,  8,  8,  8, 28, 0, 0,
		34, 34,  38, 42, 50, 34, 34,
		 0,  0,   0,  0,  0,
		34, 54,  42, 34, 34, 34, 34, 0, 0,
		28, 34,  34, 62, 34, 34, 34, 0, 0,
		62, 32,  16,  8,  4,  2, 62, 0, 0,
		62,  2,   2, 30,  2,  2, 62,
		 0,  0,   0,  0,  0,
		95, 65, 125, 69, 85, 81, 95,
	};
	private int mazeStart;
	private int mazeEnd;
	private bool mazeTapped;
	private Vector2 mazeTapPrevious;

	// Clean the whole maze
	public void MazeClean ()
	{
		for (int index = 0; index < maze.Length; ++index) {
			maze [index] = (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.BLOCK | MazeFlags.DOOR_OPENED_TOP | MazeFlags.DOOR_OPENED_LEFT);
		}
	}

	// Initialize the start point of the maze
	public void MazeInitalizeStartPoint ()
	{
		mazeStart = ((mazePathCountWidth - 1) >> 1) + (mazePathCountWidth + 1) * (mazePathCountHeight - 1);
	}

	// Create a random maze
	// TODO: Add the possibility to clean up the path (i.e. remove all dead ends), or to create a simple path (without
	// going backward when a dead end is found)?
	public int MazeCreateRandomly (int width)
	{
		// Make sure the width is correct
		width = Mathf.Clamp (width, 1, mazePathCountWidth);

		// Compute the position of the left and right borders
		int left = (mazePathCountWidth - width) >> 1;
		int right = left + width;
		int mazeStartX = mazeStart % (mazePathCountWidth + 1);
		if (mazeStartX < 0 || mazeStartX >= mazePathCountWidth || mazeStart < 0 || mazeStart >= (mazePathCountWidth + 1) * mazePathCountHeight) {
			mazeStart = ((mazePathCountWidth - 1) >> 1) + (mazePathCountWidth + 1) * ((mazePathCountHeight - 1) >> 1);
		} else if (mazeStartX < left) {
			left = mazeStartX;
			right = left + width;
		} else if (mazeStartX >= right) {
			right = mazeStartX + 1;
			left = right - width;
		}

		// Initialize the maze
		for (int x = 0; x <= mazePathCountWidth; ++x) {
			int index = x + mazePathCountHeight * (mazePathCountWidth + 1);
			byte value;
			if (x < left || x > right) {
				maze [index] = (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.BLOCK | MazeFlags.DOOR_OPENED_TOP | MazeFlags.DOOR_OPENED_LEFT);
				value = (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.BLOCK | MazeFlags.DOOR_OPENED_TOP | MazeFlags.DOOR_OPENED_LEFT);
			} else if (x == right) {
				maze [index] = (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.BLOCK | MazeFlags.DOOR_OPENED_TOP | MazeFlags.DOOR_OPENED_LEFT);
				value = (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.BLOCK | MazeFlags.DOOR_OPENED_TOP);
			} else {
				maze [index] = (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.BLOCK | MazeFlags.DOOR_OPENED_LEFT);
				value = (byte) MazeFlags.DIRECTION_INVALID;
			}
			while (index > mazePathCountWidth) {
				index -= mazePathCountWidth + 1;
				maze [index] = value;
			}
		}

		// Create the path
		int current = mazeStart;
		maze [current] |= (byte) MazeFlags.PATH;
		int length = 0;
		int lengthMax = 0;
		while (true) {
			int direction = Random.Range (0, 4);
			int rotate = 0;
			while (true) {

				// Check the neighbor
				int next = current + mazeDirectionOffset [direction];
				if (next >= 0 && next < (mazePathCountWidth + 1) * mazePathCountHeight && maze [next] == (byte) MazeFlags.DIRECTION_INVALID) {

					// Take note of this neighbor
					if (++length > lengthMax) {
						lengthMax = length;
						mazeEnd = next;
					}
					maze [next] = (byte) direction;

					// Open the door
					if (mazeDirectionOffset [direction] < 0) {
						maze [current] |= mazeDirectionOffset [direction] == -1 ? (byte) MazeFlags.DOOR_OPENED_LEFT : (byte) MazeFlags.DOOR_OPENED_TOP;
					} else {
						maze [next] |= mazeDirectionOffset [direction] == 1 ? (byte) MazeFlags.DOOR_OPENED_LEFT : (byte) MazeFlags.DOOR_OPENED_TOP;
					}

					// Continue
					current = next;
					break;
				}

				// Are there other directions to try?
				if (++rotate > 3) {
					if ((maze [current] & (byte) MazeFlags.DIRECTION_INVALID) != 0) {

						// Return the length of the path
						return lengthMax;
					}
					current -= mazeDirectionOffset [maze [current] & (byte) MazeFlags.DIRECTION_MASK];
					--length;
					break;
				}

				// Try another direction
				if (direction >= 3) {
					direction = 0;
				} else {
					++direction;
				}
			}
		}
	}

	// Create the maze using a symbol
	public void MazeCreateWithSymbol (int symbolIndex)
	{
		int index = 0;
		for (int y = 0; y <= mazePathCountHeight; ++y) {
			byte row = mazeSymbols [symbolIndex, y];
			for (int x = 0; x <= mazePathCountWidth; ++x) {
				bool path = (row & 1) != 0;
				byte value = (byte) MazeFlags.DIRECTION_INVALID;
				if (path) {
					value |= (byte) MazeFlags.PATH;
				}
				if ((x == 0 || (maze [index - 1] & (byte) MazeFlags.PATH) == 0) ^ path) {
					value |= (byte) MazeFlags.DOOR_OPENED_LEFT;
				}
				if ((y == 0 || (maze [index - mazePathCountWidth - 1] & (byte) MazeFlags.PATH) == 0) ^ path) {
					value |= (byte) MazeFlags.DOOR_OPENED_TOP;
				}
				maze [index] = value;
				row >>= 1;
				++index;
			}
		}
	}

	// Update the maze using a title
	public void MazeUpdateWithTitle (int titleIndex)
	{
		// Scroll all the blocks up
		int index = (mazePathCountHeight + 1) * (mazePathCountWidth + 1);
		while (index --> mazePathCountWidth + 1) {
			maze [index] = maze [index - (mazePathCountWidth + 1)];
		}

		// Display a row of the title on the bottom row
		byte rowCurrent = mazeTitle [titleIndex % mazeTitle.Length];
		byte rowNext = mazeTitle [(titleIndex + 1) % mazeTitle.Length];
		for (index = 0; index <= mazePathCountWidth; ++index) {
			bool path = (rowCurrent & 1) != 0;
			byte value = (byte) MazeFlags.DIRECTION_INVALID;
			if (path) {
				value |= (byte) MazeFlags.PATH;
			}
			if ((index == 0 || (maze [index - 1] & (byte) MazeFlags.PATH) == 0) ^ path) {
				value |= (byte) MazeFlags.DOOR_OPENED_LEFT;
			}
			if (((rowNext & 1) == 0) ^ path) {
				value |= (byte) MazeFlags.DOOR_OPENED_TOP;
			}
			maze [index] = value;
			rowCurrent >>= 1;
			rowNext >>= 1;
		}
	}

	// Check for taps, and update the maze accordingly
	private bool MazeUpdateWithTap (int tapX, int tapY)
	{
		// Check the tap's position
		if (tapX < 0 || tapX >= mazePathCountWidth || tapY < 0 || tapY >= mazePathCountHeight) {
			return false;
		}

		// Make sure this position is an empty corridor that is next to the path
		int indexTap = tapX + tapY * (mazePathCountWidth + 1);
		if ((maze [indexTap] & (byte) (MazeFlags.DIRECTION_INVALID | MazeFlags.PATH)) != 0) {
			return false;
		}
		int directionTap = maze [indexTap] & (byte) MazeFlags.DIRECTION_MASK;
		int indexNeighbor = indexTap - mazeDirectionOffset [directionTap];
		if ((maze [indexNeighbor] & (byte) MazeFlags.PATH) == 0) {

			// Allow to "cut corners"...
			if ((maze [indexNeighbor] & (byte) MazeFlags.DIRECTION_INVALID) != 0) {
				return false;
			}
			int directionNeighbor = maze [indexNeighbor] & (byte) MazeFlags.DIRECTION_MASK;
			if (directionTap == directionNeighbor || (maze [indexNeighbor - mazeDirectionOffset [directionNeighbor]] & (byte) MazeFlags.PATH) == 0) {
				return false;
			}

			// Set this part of the path too
			maze [indexNeighbor] |= (byte) MazeFlags.PATH;
		}

		// Set the path
		maze [indexTap] |= (byte) MazeFlags.PATH;

		// Check whether the end of the maze has been reached
		return indexTap == mazeEnd || indexNeighbor == mazeEnd;
	}

	// Check for taps, and update the maze accordingly
	public bool MazeUpdateWithTap ()
	{
		// Make sure there is a tap
		if (!Input.GetMouseButton (0)) {
			mazeTapped = false;
			return false;
		}

		// Check the tap's position
		Vector2 mazeTapEnd = new Vector2 (
			(Input.mousePosition.x - 0.5f * Screen.width) / renderGameBoardSizeBlock + 0.5f * mazePathCountWidth,
			(Input.mousePosition.y - 0.5f * Screen.height - renderGameBoardYCurrent) / renderGameBoardSizeBlock + 0.5f * mazePathCountHeight
		);

		Vector2 mazeTapBegin;
		if (mazeTapped) {
			mazeTapBegin = mazeTapPrevious;
		} else {
			mazeTapped = true;
			mazeTapBegin = mazeTapEnd;
		}
		mazeTapPrevious = mazeTapEnd;

		// Prepare to check the first cell of the segment
		int mazeTapX = Mathf.FloorToInt (mazeTapBegin.x);
		int mazeTapY = Mathf.FloorToInt (mazeTapBegin.y);
		if (mazeTapBegin == mazeTapEnd) {

			// Check whether the end of the maze has been reached
			return MazeUpdateWithTap (mazeTapX, mazeTapY);
		}

		// Prepare to check all the cells along the segment
		Vector2 mazeTapLengthInc = mazeTapEnd - mazeTapBegin;
		float mazeTapLengthMax = mazeTapLengthInc.magnitude;
		mazeTapLengthInc = new Vector2 (mazeTapLengthMax / mazeTapLengthInc.x, mazeTapLengthMax / mazeTapLengthInc.y);
		Vector2 mazeTapLength = new Vector2 (Mathf.Abs (mazeTapBegin.x), Mathf.Abs (mazeTapBegin.y));
		mazeTapLength -= new Vector2 (Mathf.Floor (mazeTapLength.x), Mathf.Floor (mazeTapLength.y));
		int mazeTapXInc;
		if (mazeTapLengthInc.x > 0.0f) {
			mazeTapLength.x = 1.0f - mazeTapLength.x;
			mazeTapXInc = 1;
		} else {
			mazeTapLengthInc.x = -mazeTapLengthInc.x;
			mazeTapXInc = -1;
		}
		int mazeTapYInc;
		if (mazeTapLengthInc.y > 0.0f) {
			mazeTapLength.y = 1.0f - mazeTapLength.y;
			mazeTapYInc = 1;
		} else {
			mazeTapLengthInc.y = -mazeTapLengthInc.y;
			mazeTapYInc = -1;
		}
		mazeTapLength.Scale (mazeTapLengthInc);

		// Check all the cells along the segment
		bool mazeEndReached = false;
		while (true) {

			// Move to the next cell
			if (mazeTapLength.x < mazeTapLength.y) {
				if (mazeTapLength.x > mazeTapLengthMax) {
					return mazeEndReached;
				}
				mazeTapX += mazeTapXInc;
				mazeTapLength.x += mazeTapLengthInc.x;
			} else {
				if (mazeTapLength.y > mazeTapLengthMax) {
					return mazeEndReached;
				}
				mazeTapY += mazeTapYInc;
				mazeTapLength.y += mazeTapLengthInc.y;
			}

			// Check whether the end of the maze has been reached
			mazeEndReached |= MazeUpdateWithTap (mazeTapX, mazeTapY);
		}
	}

	// Clear the path
	public void MazePathClear ()
	{
		// Clean the path
		int index;
		for (index = 0; index < mazePathCountHeight * (mazePathCountWidth + 1); ++index) {
			maze [index] &= (byte) MazeFlags.PATH ^ byte.MaxValue;
		}

		// Set the right path
		index = mazeEnd;
		maze [index] |= (byte) MazeFlags.PATH;
		while ((maze [index] & (byte) MazeFlags.DIRECTION_INVALID) == 0) {
			index -= mazeDirectionOffset [maze [index] & (byte) MazeFlags.DIRECTION_MASK];
			maze[index] |= (byte) MazeFlags.PATH;
		}
	}

	// Check whether the path has been cleaned
	public bool MazePathCleaned ()
	{
		return mazeStart == mazeEnd;
	}

	// Clean the path
	public void MazePathClean ()
	{
		maze [mazeStart] &= (byte) MazeFlags.PATH ^ byte.MaxValue;
		for (int direction = 0; direction < 4; ++direction) {
			int next = mazeStart + mazeDirectionOffset [direction];
			if (next >= 0 && next < (mazePathCountWidth + 1) * mazePathCountHeight && (maze [next] & (byte) (MazeFlags.DIRECTION_MASK | MazeFlags.DIRECTION_INVALID | MazeFlags.PATH)) == (direction | (byte) MazeFlags.PATH)) {
				mazeStart = next;
				return;
			}
		}
	}
}
