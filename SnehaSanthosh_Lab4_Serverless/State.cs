using Amazon.Lambda.S3Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnehaSanthosh_Lab4_Serverless
{
    /// <summary>
    /// The state passed between the step function executions.
    /// </summary>
    public class State
    {
        /// <summary>
        /// Input value when starting the execution
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// The message built through the step function execution.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The number of seconds to wait between calling the Salutations task and Greeting task.
        /// </summary>
        public int WaitInSeconds { get; set; }

        /// <summary>
        /// The validity of image based on image type
        /// </summary>
        public bool ValidImage { get; set; } = true;

        public S3Event s3Event { get; set; }
    }
}
