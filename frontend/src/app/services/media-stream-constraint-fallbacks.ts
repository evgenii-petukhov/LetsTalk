export const mediaStreamConstraintFallbacks: MediaStreamConstraints[] = [
    // HD 720p — best quality
    {
        video: {
            width: { ideal: 1280, max: 1280 },
            height: { ideal: 720, max: 720 },
            frameRate: { ideal: 30, max: 30 },
        },
        audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
            sampleRate: { ideal: 44100 },
            channelCount: { ideal: 1 },
        },
    },
    // Standard enhanced
    {
        video: {
            width: { ideal: 960, max: 960 },
            height: { ideal: 540, max: 540 },
            frameRate: { ideal: 30, max: 30 },
        },
        audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
            sampleRate: { ideal: 44100 },
            channelCount: { ideal: 1 },
        },
    },
    // Standard
    {
        video: {
            width: { ideal: 640, max: 640 },
            height: { ideal: 480, max: 480 },
            frameRate: { ideal: 30, max: 30 },
        },
        audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
            sampleRate: { ideal: 44100 },
            channelCount: { ideal: 1 },
        },
    },
    // Relaxed
    {
        video: {
            width: { ideal: 640 },
            height: { ideal: 480 },
            frameRate: { ideal: 15, max: 30 },
        },
        audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
            channelCount: { ideal: 1 },
        },
    },
    // Basic
    {
        video: {
            width: { ideal: 640 },
            height: { ideal: 480 },
        },
        audio: {
            echoCancellation: true,
        },
    },
    // Simple
    {
        video: true,
        audio: true,
    },
];
