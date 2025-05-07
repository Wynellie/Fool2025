using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CardFool
{
    public class MPlayer2
    {
        private string Name = "2.0";
        private List<SCard> hand = new List<SCard>();       // карты на руке
        private Suits trumpSuit = new Suits();
        private List<SCard> trumpsInHand = new List<SCard>();
        List<SCard> cardsInGame = new List<SCard>(); // карты в игре
        int DumpCards = 0; // Количество кард в бито
        int oppHas = 6;
        bool isLastMoveMy = true;

        // константы штрафов

        public double rankK = 0.255; //меняем в диапазоне от 0.4 до 3   1 - дефолт
        private double rankKLimit = 2;

        public double pairK = 27; // меняем в диапазоне от 5 до 20   13
        private double pairKLimit = 19.5;

        public double newCardK = 11.5; // меняем в диапазоне от 2 до 6   4
        private double newCarkLimit = 8;

        public void Reset()
        {
            hand.Clear();
            trumpSuit = new Suits();
            trumpsInHand.Clear();
            cardsInGame.Clear();
            DumpCards = 0; oppHas = 6;
            isLastMoveMy = true;
        }

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

            isLastMoveMy = true;
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

                List<int> defendersIndexes = new List<int>(); // индексы карт, из которых выбираем ту, которой будем биться
                bool pairBeaten = false;
                for (int j = 0; j < hand.Count; j++)  // пытаемся отбить карту без козырей
                {
                    if (SCard.CanBeat(table[i].Down, hand[j], trumpSuit))
                    {
                        defendersIndexes.Add(j);
                        pairBeaten = true;
                    }
                }
                if (pairBeaten)
                {
                    int index = ChooseByScores(defendersIndexes, table[i].Down, table);
                    SCardPair updatedPair = table[i]; // Создаём копию
                    updatedPair.SetUp(hand[index], trumpSuit); // Обновляем копию
                    hand.RemoveAt(index);
                    table[i] = updatedPair; // Присваиваем обратно в список
                }



                if (!pairBeaten)  // пытаемся отбиться если не получилось без козыре
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

            isLastMoveMy = false;
            return defendable;
        }

        // функция прнимает список indexes индексов карт в руке, которые могут по правилам побить карту соперника oppCard
        // возвращает индекс карты В РУКЕ, соответствующий карте с наибольшим количеством очков
        private int ChooseByScores(List<int> indexes, SCard oppCard, List<SCardPair> table)
        {

            if (indexes.Count == 1) return indexes.First();



            Dictionary<int, double> penalties = new Dictionary<int, double>(); //словарь: индекс карты - ее балл

            // заполняем словарь, ключ - индекс в руке, значение = штраф за ранг
            foreach (int i in indexes)
            {
                penalties.Add(i, hand[i].Rank * rankK);
            }

            // добавляем штраф за наличие пары в руке в данный момент,
            // можно переписать так, чтоб был разный штраф за пару, тройку и четверку
            foreach (int i in indexes) // прозодимся по картам, index - индекс в руке
            {
                foreach (SCard ownCard in hand)
                {
                    if (ownCard.Rank == hand[i].Rank && ownCard.Suit != hand[i].Suit)
                    {
                        penalties[i] += pairK;
                    }
                }
            }

            //добавляем штраф за карты на столе

            //Заполняем список лежащих на столе рангов
            if (table.Count() != 5)
            {
                List<int> ranksOnTable = new List<int>();
                foreach (SCardPair pair in table)
                {
                    ranksOnTable.Add(pair.Down.Rank);
                    if (pair.Beaten) ranksOnTable.Add(pair.Up.Rank);
                }

                // проходимся по картам, если они не представлены на столе - штраф
                foreach (int i in indexes)
                {
                    bool isPresent = false;
                    foreach (int rank in ranksOnTable)
                    {
                        if (hand[i].Rank == rank)
                        {
                            isPresent = true;
                            break;
                        }
                    }
                    if (isPresent) //если карта уже есть на столе, то штраф не начисляем
                    {
                        break;
                    }
                    // добавляем штраф за то, насколько много в соотношении к имеющимся картам осталось таких карт
                    // (насколько вероятно, что у соперника прямо сейчас на руках карта с тем же рангом, помноженная на константу)
                    //ищем карту с наименьшим штрафом и возвращаем индекс этой карты в руке

                    int cardsOfRank = 0; // количество карт такого же ранга в игре (у соперника и в колоде)
                                         // перебор потом сделать другой, негоже проходиться по всей нахуй колоде
                    foreach (SCard cardInGame in cardsInGame)
                    {
                        if (cardInGame.Rank == hand[i].Rank) cardsOfRank++;
                    }
                    penalties[i] += newCardK * cardsOfRank * 1 / cardsInGame.Count();
                    // надо учесть количество карт в руке 
                }
            }



            int result = indexes[0];
            for (int i = 1; i < indexes.Count; i++)
            {
                if (penalties[result] > penalties[indexes[i]]) result = indexes[i];
            }


            return result;
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

            if (IsDefenceSuccesful)
            {
                foreach (SCardPair legacyPair in table)
                {
                    DumpCards += 2;
                    cardsInGame.Remove(legacyPair.Up);
                    cardsInGame.Remove(legacyPair.Down);
                }
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

        // меняет константы, принимает номер константы, которую надо поменять
        // округляем, чтоб 1,4 не превращалось в 1,4000000000000001
        public void SetConst(double c1, double c2, double c3)
        {
            rankK = Math.Round(c1,3); 
            pairK = Math.Round(c2, 3);
            newCardK = Math.Round(c3, 3);
        }
    }
}
