using System;
using System.Collections.Generic;

namespace CardFool
{
    public class MPlayer1
    {
        private string Name = "1.0";
        private List<SCard> hand = new List<SCard>();// карты на руке
        private Suits trumpSuit = new Suits();
        private List<SCard> trumpsInHand = new List<SCard>();
        List<SCard> cardsInGame = new List<SCard>(); // карты в игре
        int DumpCards = 0; // Количество кард в бито

        // Возвращает имя игрока
        public string GetName()
        {
            return Name;
        }
        //Возвращает количество карт на руке
        public int GetCount()
        {
            return hand.Count + trumpsInHand.Count;
        }
        //Добавление карты в руку, во время добора из колоды, или взятия карт
        public void AddToHand(SCard card)
        {

            if (card.Suit == trumpSuit)
            {
                trumpsInHand.Add(card);
            }
            else
            {
                hand.Add(card);
            }
        }

        //Начальная атака
        public List<SCard> LayCards()
        {
            List<SCard> attack = new List<SCard>();
            if (hand.Any())
            {
                SortByRank(hand);
                attack.Add(hand[0]);
                hand.RemoveAt(0);
            }
            else
            {
                SortByRank(trumpsInHand);
                attack.Add(trumpsInHand[0]);
                trumpsInHand.RemoveAt(0);
            }

            return attack;
        }

        //Защита от карт
        //На вход подается набор карт на столе, часть из них могут быть уже покрыты
        public bool Defend(List<SCardPair> table)
        {
            bool defendable = true;
            SortByRank(hand);
            SortByRank(trumpsInHand);

            for (int i = 0; i < table.Count; i++) // Проходимся по имеющимся парам карт
            {
                if (table[i].Beaten) continue;

                bool pairBeaten = false;
                for (int j = 0; j < hand.Count; j++)  // пытаемся отбить карту без козырей
                {
                    if (SCard.CanBeat(table[i].Down, hand[j], trumpSuit))
                    {
                        SCardPair updatedPair = table[i]; // Создаём копию
                        updatedPair.SetUp(hand[j], trumpSuit); // Обновляем копию
                        table[i] = updatedPair; // Присваиваем обратно в список
                        pairBeaten = true;
                        hand.RemoveAt(j);
                        break;
                    }
                }

                if (!pairBeaten)  // пытаемся отбиться если не получилось без козырей
                {
                    for (int j = 0; j < trumpsInHand.Count; j++)
                    {
                        if (SCard.CanBeat(table[i].Down, trumpsInHand[j], trumpSuit))
                        {
                            SCardPair updatedPair = table[i]; // Создаём копию
                            updatedPair.SetUp(trumpsInHand[j], trumpSuit); // Обновляем копию
                            table[i] = updatedPair; // Присваиваем обратно в список
                            pairBeaten = true;
                            trumpsInHand.RemoveAt(j);
                            break;
                        }
                    }
                }
                if (!pairBeaten)
                {
                    defendable = false;
                    break;
                }
            }
            return defendable;
        }
        //Добавление карт
        //На вход подается набор карт на столе, а также отбился ли оппонент
        public bool AddCards(List<SCardPair> table, bool OpponentDefenced)
        {
            bool flag = false;

            List<int> indexes = new List<int>();
            // карты в колоде и на руках противника в сумме
            int oppCards = 36 - DumpCards - table.Count() * 2 - hand.Count() - trumpsInHand.Count();

            if (table.Count() < Math.Min(6, oppCards))
            {
                foreach (SCardPair pair in table)
                {
                    for (int i = 0; i < table.Count; i++)
                    {
                        for (int j = 0; j < hand.Count; j++)
                        {
                            if (hand[j].Rank == table[i].Down.Rank || hand[j].Rank == table[i].Up.Rank)
                            {
                                flag = true;
                                table.Add(new SCardPair(hand[j]));
                                hand.RemoveAt(j);
                                return flag;
                            }
                        }
                    }
                }
            }
            return flag;
        }

        //Вызывается после основной битвы, когда известно отбился ли защищавшийся
        //На вход подается набор карт на столе, а также была ли успешной защита
        public void OnEndRound(List<SCardPair> table, bool IsDefenceSuccesful)
        {
            foreach (SCardPair legacyPair in table)
            {
                DumpCards += 2;
                cardsInGame.Remove(legacyPair.Up);
                cardsInGame.Remove(legacyPair.Down);
            }
        }
        //Установка козыря, на вход подаётся козырь, вызывается перед первой раздачей карт
        public void SetTrump(SCard NewTrump)
        {
            trumpSuit = NewTrump.Suit;
            for (int rank = 6; rank < 15; rank++)
            {
                foreach (Suits suit in Suits.GetValues(typeof(Suits)))
                {
                    cardsInGame.Add(new SCard(suit, rank));
                }
            }
        }

        private void SortByRank(List<SCard> list)
        {
            int n = list.Count;
            bool swapRequired;
            for (int i = 0; i < n - 1; i++)
            {
                swapRequired = false;
                for (int j = 0; j < n - i - 1; j++)
                    if (list[j].Rank > list[j + 1].Rank)
                    {
                        SCard tempVar = list[j];
                        list[j] = list[j + 1];
                        list[j + 1] = tempVar;
                        swapRequired = true;
                    }
                if (swapRequired == false)
                    break;
            }
        }
    }
}
