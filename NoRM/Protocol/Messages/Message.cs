﻿namespace NoRM.Protocol
{
    using SystemMessages.Responses;
    using Messages;

    public class Message
    {
        protected MongoOp _op = MongoOp.Message;
        protected IConnection _connection;
        protected string _collection;
        protected int _requestID;
        protected int _responseID;
        protected int _messageLength;


        protected Message(IConnection connection, string fullyQualifiedCollName)
        {
            _connection = connection;
            _collection = fullyQualifiedCollName;
        }

        protected void AssertHasNotError()
        {
            new QueryMessage<GenericCommandResponse, object>(_connection, _collection)
                         {
                             NumberToTake = 1,
                             Query = new {reseterror = 1d},
                         }.Execute();
        }
    }
}
