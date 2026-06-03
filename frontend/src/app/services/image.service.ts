import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class ImageService {
    resizeBase64Image(
        base64: string,
        maxWidth: number,
        maxHeight: number,
    ): Promise<Blob> {
        const t0 = performance.now();
        return new Promise<Blob>((resolve) => {
            const img = document.createElement('img');
            img.addEventListener('load', () => {
                const scaleX = img.width > maxWidth ? maxWidth / img.width : 1;
                const scaleY =
                    img.height > maxHeight ? maxHeight / img.height : 1;
                const scale = Math.min(scaleX, scaleY);

                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');

                canvas.width = img.width * scale;
                canvas.height = img.height * scale;
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                canvas.toBlob((blob: Blob) => {
                    console.log(
                        `[Image resize] ${(performance.now() - t0).toFixed(0)}ms`,
                    );
                    resolve(blob);
                }, 'image/webp');
            });
            img.src = base64;
        });
    }
}
