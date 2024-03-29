﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Specifies the different kinds of response errors.
    /// </summary>
    public enum ResponseError : byte
    {
        /// <summary>
        /// No Error
        /// </summary>
        None,
        
        /// <summary>
        /// The client does not support the specified command.
        /// </summary>
        UnknownCommand = 0x01,
        /// <summary>
        /// The client failed to validate the checksum.
        /// </summary>
        CrcMismatch = 0x02,
        /// <summary>
        /// The client expected a different parameter format.
        /// </summary>
        InvalidParameterFormat = 0x10,
        /// <summary>
        /// The client wasn't able to execute the command due to an error or invalid state.
        /// </summary>
        ExecutionFailed = 0x11,
        /// <summary>
        /// No client responded within the command timeout.
        /// </summary>
        Timeout = 0x20,
        /// <summary>
        /// InvalidResponseFormat
        /// </summary>
        InvalidResponseFormat = 0x21,
        /// <summary>
        /// Error reason not specified / unknown.
        /// </summary>
        Other = 0xFF,
    }

    /// <summary>
    /// Base class for all ModControl responses.
    /// </summary>
	public abstract class ModControlResponse
	{
        /// <summary>
        /// Indicates the error state of the response.
        /// </summary>
        public ResponseError Error { get; protected set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ModControlResponse"/> class.
        /// </summary>
        protected ModControlResponse()
        {
            Error = ResponseError.None;
        }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        protected ModControlResponse(ModControlFrame frame)
        {
            if (frame == null)
            {
                Error = ResponseError.Timeout;
                return;
            }

            // If error flag is set, command data contains error code.
            if (frame.CommandByte >= 0b10000000)
            {
                if (frame.Data.Any())
                {
                    byte enumValue = frame.Data[0];

                    if (!Enum.IsDefined(typeof(ResponseError), enumValue))
                    {
                        Error = ResponseError.Other;
                    }

                    Error = (ResponseError)enumValue;
                }
                else
                {
                    Error = ResponseError.Other;
                }
            }
            else
            {
                Error = ResponseError.None;
            }
        }

        /// <summary>
        /// Returns the payload data of the response.
        /// </summary>
        /// <returns>Payload data.</returns>
        public abstract IList<byte> GetData();
    }
}
