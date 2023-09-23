using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DOL.FTP
{
	/// <summary>
	/// Summary description for FTPAsynchronousConnection.
	/// </summary>
	public class FtpAsyncConnection : FtpConnection
	{
		private readonly Queue<string> _deleteFileQueue;
		private readonly Queue<FileTransferStruct> _getFileTransfersQueue;
		private readonly Queue<string> _makeDirQueue;
		private readonly Queue<string> _removeDirQueue;
		private readonly Queue<FileTransferStruct> _sendFileTransfersQueue;
		private readonly Queue<string> _setCurrentDirectoryQueue;

		/// <summary>
		/// Creates a new asynchronous FTP connection
		/// </summary>
		public FtpAsyncConnection()
		{
			_sendFileTransfersQueue = new Queue<FileTransferStruct>();
			_getFileTransfersQueue = new Queue<FileTransferStruct>();
			_deleteFileQueue = new Queue<string>();
			_setCurrentDirectoryQueue = new Queue<string>();
			_makeDirQueue = new Queue<string>();
			_removeDirQueue = new Queue<string>();
		}

		/// <summary>
		/// Retrieves a remote file
		/// </summary>
		/// <param name="remoteFileName">The remote filename</param>
		/// <param name="type">The transfer type</param>
		public override void GetFile(string remoteFileName, EFtpFileTransferType type)
		{
			GetFile(remoteFileName, Path.GetFileName(remoteFileName), type);
		}

		/// <summary>
		/// Retrieves a remote file
		/// </summary>
		/// <param name="remoteFileName">The remote filename</param>
		/// <param name="localFileName">The local filename</param>
		/// <param name="type">The transfer type</param>
		public override void GetFile(string remoteFileName, string localFileName, EFtpFileTransferType type)
		{
			var ftStruct = new FileTransferStruct
			               	{
			               		LocalFileName = localFileName,
			               		RemoteFileName = remoteFileName,
			               		Type = type
			               	};

			_getFileTransfersQueue.Enqueue(ftStruct);

			ThreadPool.QueueUserWorkItem(GetFileFromQueue);
		}

		private void GetFileFromQueue(object state)
		{
			FileTransferStruct ftStruct = _getFileTransfersQueue.Dequeue();
			base.GetFile(ftStruct.RemoteFileName, ftStruct.LocalFileName, ftStruct.Type);
		}

		/// <summary>
		/// Sends a file to the remote host
		/// </summary>
		/// <param name="localFileName">The local filename</param>
		/// <param name="type">The transfer type</param>
		public override void SendFile(string localFileName, EFtpFileTransferType type)
		{
			SendFile(localFileName, Path.GetFileName(localFileName), type);
		}

		/// <summary>
		/// Sends a file to the remote host
		/// </summary>
		/// <param name="localFileName">The local filename</param>
		/// <param name="remoteFileName">The remote filename</param>
		/// <param name="type">The transfer type</param>
		public override void SendFile(string localFileName, string remoteFileName, EFtpFileTransferType type)
		{
			var ftStruct = new FileTransferStruct
			               	{
			               		LocalFileName = localFileName,
			               		RemoteFileName = remoteFileName,
			               		Type = type
			               	};

			_sendFileTransfersQueue.Enqueue(ftStruct);

			ThreadPool.QueueUserWorkItem(SendFileFromQueue);
		}

		private void SendFileFromQueue(object state)
		{
			FileTransferStruct ftStruct = _sendFileTransfersQueue.Dequeue();
			base.SendFile(ftStruct.LocalFileName, ftStruct.RemoteFileName, ftStruct.Type);
		}

		/// <summary>
		/// Deletes a remote file
		/// </summary>
		/// <param name="remoteFileName">The remote filename</param>
		public override void DeleteFile(string remoteFileName)
		{
			_deleteFileQueue.Enqueue(remoteFileName);

			ThreadPool.QueueUserWorkItem(DeleteFileFromQueue);
		}

		private void DeleteFileFromQueue(object state)
		{
			base.DeleteFile(_deleteFileQueue.Dequeue());
		}

		/// <summary>
		/// Sets the current remote directory
		/// </summary>
		/// <param name="remotePath">The remote path to set</param>
		public override void SetCurrentDirectory(string remotePath)
		{
			_setCurrentDirectoryQueue.Enqueue(remotePath);

			ThreadPool.QueueUserWorkItem(SetCurrentDirectoryFromQueue);
		}

		private void SetCurrentDirectoryFromQueue(object state)
		{
			base.SetCurrentDirectory(_setCurrentDirectoryQueue.Dequeue());
		}

		/// <summary>
		/// Creates a directory on the remote server
		/// </summary>
		/// <param name="directoryName">The directory name to create</param>
		public override void CreateDirectory(string directoryName)
		{
			_makeDirQueue.Enqueue(directoryName);

			ThreadPool.QueueUserWorkItem(MakeDirFromQueue);
		}

		private void MakeDirFromQueue(object state)
		{
			base.CreateDirectory(_makeDirQueue.Dequeue());
		}

		/// <summary>
		/// Removes a remote directory
		/// </summary>
		/// <param name="directoryName">The directory name to remove</param>
		public override void RemoveDirectory(string directoryName)
		{
			_removeDirQueue.Enqueue(directoryName);

			ThreadPool.QueueUserWorkItem(RemoveDirFromQueue);
		}

		private void RemoveDirFromQueue(object state)
		{
			base.RemoveDirectory(_removeDirQueue.Dequeue());
		}

		#region Nested type: FileTransferStruct

		private struct FileTransferStruct
		{
			public string LocalFileName;
			public string RemoteFileName;
			public EFtpFileTransferType Type;
		}

		#endregion
	}
}