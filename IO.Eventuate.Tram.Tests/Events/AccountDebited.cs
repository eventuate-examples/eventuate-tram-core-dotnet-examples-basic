using IO.Eventuate.Tram.Events.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Eventuate.Tram.Tests
{
    public class AccountDebited : IDomainEvent
    {
        public long MyAmount;
        public AccountDebited()
        {
        }
        public AccountDebited(long amount)
        {
            MyAmount = amount;
        }
        public long getAmount()
        {
            return MyAmount;
        }
        public void setAmount(long amount)
        {
            MyAmount = amount;
        }
    }
}
