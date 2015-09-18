using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace SignalRedisBugRepro.vNext.Hubs
{
   public class HyperHub : Hub
   {
      public async Task SendAlotThenJoin()
      {
         // subtract 3 from this number, and it will succeed every time! (assuming you are on 64-bit architecture!)
         long messagesToSend = 1015808;

         long count = 0;
         var runningTasks = new List<Task>();

         // 1015808 is MaxMessages in ring buffer on 64-bit system, based on calculations made in ScaleoutStore
         // 1007616 is MaxMessages in ring buffer on 32-bit system, based on calculations made in ScaleoutStore
         while ( count < messagesToSend )
         {
            count++;

            // Run up to 200 tasks concurrently, so we can finish this job as fast as possible
            if ( runningTasks.Count >= 200 )
            {
               var t = await Task.WhenAny( runningTasks ).ConfigureAwait( false );
               runningTasks.Remove( t );
            }

            // just send some random message
            // WARNING: This might sometimes fail in an REDIS error, which is unrelated to my issue!
            var task = Clients.Group( "some-group" ).OnUpdate( new
            {
               Id = Guid.NewGuid(),
            } );
            runningTasks.Add( task );

            // trace out, so we have a rough idea of how far we are in the process
            if ( count % 1000 == 0 )
            {
               Trace.WriteLine( count );
            }
         }

         // wait for all to finish
         await Task.WhenAll( runningTasks ).ConfigureAwait( false );

         // This will hang, if we send too many messages!
         await Groups.Add( Context.ConnectionId, "some-other-group" ).ConfigureAwait( false );
      }
   }
}
