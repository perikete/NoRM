﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Norm.BSON;

namespace Norm.Protocol.Messages
{
    /// <summary>
    /// The reply message.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public class ReplyMessage<T> : Message, IDisposable
    {
        private List<T> _results = null;
        private int _limit;
        private MongoOp _originalOperation;
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplyMessage{T}"/> class.
        /// Processes a response stream.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="fullyQualifiedCollestionName">The fully Qualified Collestion Name.</param>
        /// <param name="reply">The reply.</param>
        /// <param name="originalOperation"></param>
        /// <param name="limit"></param>
        internal ReplyMessage(IConnection connection, string fullyQualifiedCollestionName, BinaryReader reply, MongoOp originalOperation, int limit)
            : base(connection, fullyQualifiedCollestionName)
        {
            this._originalOperation = originalOperation;
            this._messageLength = reply.ReadInt32();
            this._requestID = reply.ReadInt32();
            this._responseID = reply.ReadInt32();
            this._op = (MongoOp)reply.ReadInt32();
            this._limit = limit;
            this.HasError = reply.ReadInt32() == 1 ? true : false;
            this.CursorID = reply.ReadInt64();
            this.CursorPosition = reply.ReadInt32();

            var count = reply.ReadInt32();

            // decrement the length for all the reads.
            _messageLength -= 4 + 4 + 4 + 4 + 4 + 4 + 8 + 4 + 4;

            _results = new List<T>(count);

            if (this.HasError)
            {
                // TODO: load the error document.
            }
            else
            {
                while (_messageLength > 0)
                {
                    var length = reply.ReadInt32();
                    if (length > 0)
                    {
                        var bin = BitConverter.GetBytes(length).Concat(
                            reply.ReadBytes(length - 4)).ToArray();

                        IDictionary<WeakReference, Flyweight> outProps = new Dictionary<WeakReference, Flyweight>(0);
                        var obj = BsonDeserializer.Deserialize<T>(bin, ref outProps);
                        this._results.Add(obj);
                        if (_connection.EnableExpandoProperties)
                        {
                            ExpandoProps.SetFlyWeightObjects(outProps);
                        }
                    }

                    _messageLength -= length;
                }
            }
        }

        /// <summary>
        /// The cursor to be used in future calls to "get more"
        /// </summary>
        public long CursorID { get; protected set; }

        /// <summary>
        /// The location of the cursor.
        /// </summary>
        public int CursorPosition { get; protected set; }

        /// <summary>
        /// If "HasError" is set, 
        /// </summary>
        public bool HasError { get; protected set; }

        private ReplyMessage<T> _addedReturns = null;

        /// <summary>
        /// Gets enumerable results.
        /// </summary>
        public IEnumerable<T> Results
        {
            get
            {
                foreach (var r in this._results)
                {
                    yield return r;
                }
                if (this.CursorID != 0 && this._results.Count > 0 && this._limit - this._results.Count > 0)
                {
                    this._addedReturns = new GetMoreMessage<T>(this._connection, 
                        this._collection, this.CursorID, this._limit - this._results.Count).Execute();
                }
                if (this._addedReturns != null)
                {
                    foreach (var r in this._addedReturns.Results)
                    {
                        yield return r;
                    }
                }
                yield break;
            }
        }

        #region IDisposable Members

        /// <summary>TODO::Description.</summary>
        public void Dispose()
        {
            //this should kill the cursor if it exists.
        }

        #endregion
    }
}