/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import { ImageService } from './image.service';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('ImageService', () => {
    let service: ImageService;

    let mockCanvas: any;
    let mockContext: CanvasRenderingContext2D;
    let mockBlob: Blob;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [ImageService],
        });
        service = TestBed.inject(ImageService);

        // Create a proper mock canvas that tracks width and height
        mockCanvas = {
            _width: 0,
            _height: 0,
            get width() {
                return this._width;
            },
            set width(value) {
                this._width = value;
            },
            get height() {
                return this._height;
            },
            set height(value) {
                this._height = value;
            },
            getContext: vi.fn(),
            toBlob: vi.fn(),
        };

        mockContext = {
            drawImage: vi.fn(),
        } as unknown as CanvasRenderingContext2D;

        mockCanvas.getContext.mockReturnValue(mockContext);

        vi.spyOn(document, 'createElement').mockImplementation(
            (element: string) => {
                if (element === 'canvas') {
                    return mockCanvas;
                }
                if (element === 'img') {
                    const img = {
                        addEventListener: vi.fn(
                            (event: string, handler: () => void) => {
                                if (event === 'load') {
                                    setTimeout(() => {
                                        Object.defineProperty(img, 'width', {
                                            value: 300,
                                            writable: true,
                                        });
                                        Object.defineProperty(img, 'height', {
                                            value: 200,
                                            writable: true,
                                        });
                                        handler();
                                    }, 0);
                                }
                            },
                        ),
                        src: '',
                        width: 300,
                        height: 200,
                    };
                    return img as any;
                }
                return document.createElement(element);
            },
        );

        mockBlob = new Blob();
        mockCanvas.toBlob.mockImplementation(
            (callback: (blob: Blob) => void) => {
                callback(mockBlob);
            },
        );
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('resizeBase64Image', () => {
        it('should resize an image and return a Blob', async () => {
            // Arrange
            const base64Image = 'data:image/jpeg;base64,some-base64-data';
            const maxWidth = 100;
            const maxHeight = 100;

            // Act
            const resizedBlob = await service.resizeBase64Image(
                base64Image,
                maxWidth,
                maxHeight,
            );

            // Assert
            expect(resizedBlob).toBe(mockBlob);
            expect(mockCanvas.width).toBeLessThanOrEqual(maxWidth);
            expect(mockCanvas.height).toBeLessThanOrEqual(maxHeight);
            expect(mockContext.drawImage).toHaveBeenCalled();
            expect(mockCanvas.toBlob).toHaveBeenCalledWith(
                expect.any(Function),
                'image/webp',
            );
        });

        it('should maintain aspect ratio while resizing', async () => {
            // Arrange
            const base64Image = 'data:image/jpeg;base64,some-base64-data';
            const maxWidth = 150;
            const maxHeight = 100;

            // Act
            await service.resizeBase64Image(base64Image, maxWidth, maxHeight);

            // Assert
            const expectedScale = Math.min(maxWidth / 300, maxHeight / 200);
            expect(mockCanvas.width).toBe(300 * expectedScale);
            expect(mockCanvas.height).toBe(200 * expectedScale);
        });
    });
});
