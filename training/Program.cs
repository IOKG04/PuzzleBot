using System.Diagnostics.CodeAnalysis;
using pax.chess;

namespace training;

class Program{
    //Puzzles
    static float[][][] startingPositions;	//List of all starting positions in bit boards + en passant representation (1 - On | -1 - Off)
    static Move[][] moves;			//List of all moves (even ones are played by opponent)
    static string[] puzzles;			//List of all puzzels in Lichess format

    static void Main(string[] args){
    }
}

//Format for storing square positions:
//(Rank number - 1) + ((File letter - 1) * 8)
//Exaples: A1 = 0, A2 = 1, B1 = 8, D6 = 37, H8 = 63

struct Puzzle{
    //Board
    public float[][] bitBoards;	//List of bit board for all pieces/colors (1 = On, -1 = Off)
				//[0] : White Pawns
				//[1] : White Knights
				//[2] : White Bishops
				//[3] : White Rooks
				//[4] : White Queens
				//[5] : White King(s)
				//Black is [same + 6]
    public float[] enPassant;	//List of files in which en passant can be performed (Rank 2 = 0, Rank 7 = 8) (A file = +0, B file = +1, etc.) (1 = On, -1 = Off)
    public float[] castling;	//(1 = On, -1 = Off)
				//0 : White Kingside, 1 : White Queenside, 2 : Black Kingside, 3 : Black Queenside
    public float activeColor;	//Indicatoe of which color's turn it is (1 = White, -1 = Black)
    public float halfmoveClock;	//Amount of half moves since last capture or pawn advance (for fifty-move rule)

    public Move solution;	//Solution to the puzzle

    public Puzzle(string _startingPosition_FEN, Move[] _preMoves, Move _solution){
    }

    private void ParseFEN(string FEN){
	string[] fields = FEN.Split(' ', StringSplitOptions.RemoveEmptyEntries);

	//Bit boards
	bitBoards = new float[12][];
	for(int i = 0; i < bitBoards.Length; i++){
	    bitBoards[i] = new float[64];
	    for(int j = 0; j < bitBoards[i].Length; j++){
		bitBoards[i][j] = -1;
	    }
	}
	int rank = 8, file = -1;
	foreach(char c in fields[0]){
	    switch(c){
		case '/':
		    rank--;
		    file = -1;
		    break;
		case 'P':
		    file++;
		    bitBoards[0][file * 8 + rank] = 1;
		    break;
		case 'N':
		    file++;
		    bitBoards[1][file * 8 + rank] = 1;
		    break;
		case 'B':
		    file++;
		    bitBoards[2][file * 8 + rank] = 1;
		    break;
		case 'R':
		    file++;
		    bitBoards[3][file * 8 + rank] = 1;
		    break;
		case 'Q':
		    file++;
		    bitBoards[4][file * 8 + rank] = 1;
		    break;
		case 'K':
		    file++;
		    bitBoards[5][file * 8 + rank] = 1;
		    break;
		case 'p':
		    file++;
		    bitBoards[6][file * 8 + rank] = 1;
		    break;
		case 'n':
		    file++;
		    bitBoards[7][file * 8 + rank] = 1;
		    break;
		case 'b':
		    file++;
		    bitBoards[8][file * 8 + rank] = 1;
		    break;
		case 'r':
		    file++;
		    bitBoards[9][file * 8 + rank] = 1;
		    break;
		case 'q':
		    file++;
		    bitBoards[10][file * 8 + rank] = 1;
		    break;
		case 'k':
		    file++;
		    bitBoards[11][file * 8 + rank] = 1;
		    break;
		default:
		    file += int.Parse(c.ToString());
		    break;
	    }
	}
	
	//Active color
	activeColor = fields[1] == "w" ? 1 : -1;
	
	//Castling
	castling = new float[4];
	castling[0] = fields[2].Contains('K') ? 1 : -1;
	castling[1] = fields[2].Contains('Q') ? 1 : -1;
	castling[2] = fields[2].Contains('k') ? 1 : -1;
	castling[3] = fields[2].Contains('q') ? 1 : -1;

	//En passant
	fields[3] = fields[3].ToUpper();
	enPassant = new float[16];
	enPassant[0] = fields[3] == "A2" ? 1 : -1;
	enPassant[1] = fields[3] == "B2" ? 1 : -1;
	enPassant[2] = fields[3] == "C2" ? 1 : -1;
	enPassant[3] = fields[3] == "D2" ? 1 : -1;
	enPassant[4] = fields[3] == "E2" ? 1 : -1;
	enPassant[5] = fields[3] == "F2" ? 1 : -1;
	enPassant[6] = fields[3] == "G2" ? 1 : -1;
	enPassant[7] = fields[3] == "H2" ? 1 : -1;
	enPassant[8] = fields[3] == "A7" ? 1 : -1;
	enPassant[9] = fields[3] == "B7" ? 1 : -1;
	enPassant[10] = fields[3] == "C7" ? 1 : -1;
	enPassant[11] = fields[3] == "D7" ? 1 : -1;
	enPassant[12] = fields[3] == "E7" ? 1 : -1;
	enPassant[13] = fields[3] == "F7" ? 1 : -1;
	enPassant[14] = fields[3] == "G7" ? 1 : -1;
	enPassant[15] = fields[3] == "H7" ? 1 : -1;

	//Halfmove clock
	halfmoveClock = float.Parse(fields[4]);
    }
}

struct Move{
    public byte start, end;			//Start and end positions in notation explained above
    public PromotionPiece promotionPiece;	//Piece to promote to 

    public static bool operator ==(Move a, Move b){
	return a.start == b.start && a.end == b.end && (a.promotionPiece & b.promotionPiece) != 0;
    }
    public static bool operator !=(Move a, Move b){
	return !(a == b);
    }

    public Move(byte _start, byte _end){
	start = _start;
	end = _end;
	promotionPiece = PromotionPiece.DontCare;
    }
    public Move(byte _start, byte _end, PromotionPiece _promotionPiece){
	start = _start;
	end = _end;
	promotionPiece = _promotionPiece;
    }
    public Move(string UCI){
	UCI = UCI.ToUpper();
	start = (byte)(byte.Parse(UCI[1].ToString()) + (((byte)UCI[0] - 65) * 8));
	end = (byte)(byte.Parse(UCI[3].ToString()) + (((byte)UCI[2] - 65) * 8));
	promotionPiece = PromotionPiece.DontCare;
	if(UCI.EndsWith('Q')) promotionPiece = PromotionPiece.Queen;
	else if(UCI.EndsWith('R')) promotionPiece = PromotionPiece.Rook;
	else if(UCI.EndsWith('B')) promotionPiece = PromotionPiece.Bishop;
	else if(UCI.EndsWith('N')) promotionPiece = PromotionPiece.Knight;
    }
}

//Possible pieces to promote to
//Compare using binary and operator
//If not applicable, use DontCare
enum PromotionPiece : byte{
    Bishop	= 0b0001,
    Knight	= 0b0010,
    Rook	= 0b0100,
    Queen	= 0b1000,
    DontCare	= 0b1111,	//Matches all if compared using binary and operator
}
