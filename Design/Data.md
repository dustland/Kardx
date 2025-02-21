# System Data Design

## Basic Design

### Data Layer

The data layer is responsible for storing and managing the data of the game.

### Logic Layer

The logic layer is responsible for providing the logic of the game, handling the rules and the game flow.

### UI Layer

The UI layer is responsible for displaying the data to the player.

## 1. Card Abstraction

Our design abstracts each card as a modular entity with the following core attributes:

```cs
public class CardData
{
    public string id; // unique identifier
    public string name; // name of the card
    public string description; // description of the card
    public string type; // creature, spell, equipment
    public string subtype; // subtype of the card
    public int attack; // attack of the card
    public int defense; // defense of the card
    public int magic;
    public int price;
    public int points; // cost of using this card
    public string image;
    public string rarity;
    public string set;
}
```

## 2. Board State

The board state is responsible for storing the state of the board, i.e. the cards on the board, the cards in the hand, the cards in the deck, etc.

```cs
public class BoardState
{
    public List<CardData> cards;
    public List<CardData> hand;
    public List<CardData> deck;
    public List<CardData> graveyard; // cards that have been removed from the game
    public List<CardData> board; // cards that are on the board
}
```
