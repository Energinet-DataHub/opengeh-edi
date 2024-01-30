// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

public class MarketDocument
{
    public static readonly FileStorageCategory FileStorageCategory = ArchivedFile.FileStorageCategory; // Market Document uses the ArchivedMessage's file in file storage

    private Stream _document;

    /// <summary>
    /// Create a market document from a bundle and an archived file. <see cref="IArchivedFile"/> should be created/retrieved by an IArchivedMessagesClient in our ArchivedMessages module
    /// </summary>
    /// <param name="bundleId">The <see cref="BundleId"/> is the bundle id from an <seealso cref="ActorMessageQueue"/></param>
    /// <param name="archivedFile">An archived file created/retrieved by an IArchivedMessagesClient in our ArchivedMessages module</param>
    public MarketDocument(BundleId bundleId, IArchivedFile archivedFile)
    {
        ArgumentNullException.ThrowIfNull(archivedFile);

        Id = Guid.NewGuid();
        BundleId = bundleId;

        FileStorageReference = archivedFile.FileStorageReference;
        _document = archivedFile.Document;
    }

    /// <summary>
    /// Should only be used by Entity Framework
    /// </summary>
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private MarketDocument(BundleId bundleId, FileStorageReference fileStorageReference)
    {
        BundleId = bundleId;
        FileStorageReference = fileStorageReference;
        _document = null!; // Set later in MarketDocumentRepository, by getting the document from File Storage
    }

    public Guid Id { get; }

    public BundleId BundleId { get; }

    public FileStorageReference FileStorageReference { get; }

    public void SetDocument(Stream document)
    {
        _document = document;
    }

    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Can cause error as a property because of serialization and message record maybe being null at the time")]
    public Stream GetDocument() => _document;
}
