/* eslint-disable @typescript-eslint/no-explicit-any */
import { vi } from 'vitest';
import { getTestBed } from '@angular/core/testing';
import {
    BrowserTestingModule,
    platformBrowserTesting,
} from '@angular/platform-browser/testing';

// Initialize Angular testing environment
getTestBed().initTestEnvironment(
    BrowserTestingModule,
    platformBrowserTesting(),
);

// Mock browser APIs that are not available in jsdom
Object.defineProperty(window, 'Notification', {
    value: class MockNotification {
        static permission = 'granted';
        static requestPermission = vi.fn().mockResolvedValue('granted');
    },
    writable: true,
});

Object.defineProperty(navigator, 'mediaDevices', {
    value: {
        getUserMedia: vi.fn().mockResolvedValue({
            getTracks: () => [],
            getVideoTracks: () => [],
            getAudioTracks: () => [],
        }),
    },
    writable: true,
});

// Mock MediaStream
Object.defineProperty(window, 'MediaStream', {
    value: class MockMediaStream {
        private tracks: MediaStreamTrack[];
        constructor(tracks?: MediaStreamTrack[]) {
            this.tracks = tracks ?? [];
        }
        getTracks = vi.fn(() => this.tracks);
        getVideoTracks = vi.fn(() => this.tracks.filter((t) => t.kind === 'video'));
        getAudioTracks = vi.fn(() => this.tracks.filter((t) => t.kind === 'audio'));
        addTrack = vi.fn();
        removeTrack = vi.fn();
    },
    writable: true,
});

// Enhanced RTCPeerConnection mock for better test compatibility
Object.defineProperty(window, 'RTCPeerConnection', {
    value: class MockRTCPeerConnection {
        localDescription = null;
        remoteDescription = null;
        signalingState = 'stable';
        connectionState = 'new';

        setConfiguration = vi.fn();
        createOffer = vi.fn().mockResolvedValue({});
        createAnswer = vi.fn().mockResolvedValue({});
        setLocalDescription = vi.fn().mockResolvedValue(undefined);
        setRemoteDescription = vi.fn().mockResolvedValue(undefined);
        addIceCandidate = vi.fn().mockResolvedValue(undefined);
        addTrack = vi.fn();
        close = vi.fn();

        onicecandidate = null;
        ontrack = null;
        onconnectionstatechange = null;

        addEventListener = vi.fn();
        removeEventListener = vi.fn();

        constructor() {
            // Mock constructor
        }

        static generateCertificate = vi.fn();
    },
    writable: true,
});

// Mock localStorage
Object.defineProperty(window, 'localStorage', {
    value: {
        getItem: vi.fn(),
        setItem: vi.fn(),
        removeItem: vi.fn(),
        clear: vi.fn(),
        length: 0,
        key: vi.fn(),
    },
    writable: true,
});

// Mock Blob with arrayBuffer method and proper _buffer property
global.Blob = class MockBlob {
    _buffer: ArrayBuffer;

    constructor(parts?: BlobPart[]) {
        // Create a proper ArrayBuffer for the mock
        this._buffer = new ArrayBuffer(8);
        if (parts && parts.length > 0) {
            // Simulate content based on parts
            const totalLength = parts.reduce((acc, part) => {
                if (typeof part === 'string') return acc + part.length;
                if (part instanceof ArrayBuffer) return acc + part.byteLength;
                return acc + 8; // default size
            }, 0);
            this._buffer = new ArrayBuffer(totalLength);
        }
    }

    arrayBuffer(): Promise<ArrayBuffer> {
        return Promise.resolve(this._buffer);
    }

    slice(): Blob {
        return new MockBlob();
    }

    stream(): ReadableStream {
        return new ReadableStream();
    }

    text(): Promise<string> {
        return Promise.resolve('');
    }
} as any;

// Mock File with arrayBuffer method
global.File = class MockFile extends global.Blob {
    name: string;
    lastModified: number;

    constructor(parts: BlobPart[], name: string, options?: FilePropertyBag) {
        super(parts, options);
        this.name = name;
        this.lastModified = Date.now();
    }
} as any;

// Also set the static methods directly on the constructor
(window.URL as any).createObjectURL = vi.fn().mockReturnValue('blob:mock-url');
(window.URL as any).revokeObjectURL = vi.fn();
