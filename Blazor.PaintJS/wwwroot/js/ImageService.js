let image = null;

export async function createImage(stream) {
    resetImage();
    const arrayBuffer = await stream.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    return createImageElement(blob);
}

export function downloadImage(dataUrl) {
    const link = document.createElement("a");
    link.href = dataUrl;
    link.download = 'image.png';
    link.click();
}

export async function getCanvasImageData(referenceCanvasId) {    
        return new Promise(async (resolve, reject) => {
            try {
                const referenceCanvas = document.getElementById(referenceCanvasId);
                if (referenceCanvas) {
                    await referenceCanvas.toBlob(async blob => {
                        const buffer = await blob.arrayBuffer();
                        const arrayData = new Uint8Array(buffer);
                        resolve(arrayData);
                    });
                } else {
                    resolve(new Uint8Array([]));
                }
            } catch (err) {
                reject(err);
            }
        });
    
}

export async function openNewFile(imageBlob) {
    resetImage();
    return createImageElement(imageBlob)
}

export async function generateFile(dataUrl) {
    const blob = await (await fetch(dataUrl)).blob();
    return new File([blob], 'paint.png', { type: 'image/png' });
}

function createImageElement(blob, returnValue) {
    return new Promise((resolve, reject) => {
        const imageRef = new Image();
        imageRef.onload = () => {
            image = document.getElementById(imageRef.id);
            resolve(returnValue);
        };
        imageRef.onerror = () => {
            imageRef.remove();
            reject();
        };
        imageRef.src = URL.createObjectURL(blob);
        imageRef.style.display = 'none';
        imageRef.id = 'image';

        document.body.appendChild(imageRef);
    });
}

function resetImage() {
    if (image) {
        image.remove();
        image = null;
    }
}