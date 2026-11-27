import type { Area } from 'react-easy-crop'

const OUTPUT_AVATAR_SIZE = 512

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.addEventListener('load', () => resolve(img))
    img.addEventListener('error', () => reject(new Error('Không thể đọc dữ liệu ảnh để cắt.')))
    img.src = src
  })
}

function getRotatedBoundingSize(width: number, height: number, rotationDeg: number) {
  const rad = (rotationDeg * Math.PI) / 180
  return {
    width: Math.abs(Math.cos(rad) * width) + Math.abs(Math.sin(rad) * height),
    height: Math.abs(Math.sin(rad) * width) + Math.abs(Math.cos(rad) * height),
  }
}

async function renderCroppedCanvas(
  imageSrc: string,
  pixelCrop: Area,
  rotationDeg = 0,
  outputSize = OUTPUT_AVATAR_SIZE,
): Promise<HTMLCanvasElement> {
  const image = await loadImage(imageSrc)
  const rad = (rotationDeg * Math.PI) / 180
  const { width: rotatedWidth, height: rotatedHeight } = getRotatedBoundingSize(
    image.width,
    image.height,
    rotationDeg,
  )

  const tempCanvas = document.createElement('canvas')
  tempCanvas.width = Math.round(rotatedWidth)
  tempCanvas.height = Math.round(rotatedHeight)
  const tempCtx = tempCanvas.getContext('2d')
  if (!tempCtx) {
    throw new Error('Trình duyệt không hỗ trợ xử lý ảnh Canvas.')
  }

  tempCtx.translate(tempCanvas.width / 2, tempCanvas.height / 2)
  tempCtx.rotate(rad)
  tempCtx.translate(-image.width / 2, -image.height / 2)
  tempCtx.drawImage(image, 0, 0)

  const outCanvas = document.createElement('canvas')
  outCanvas.width = outputSize
  outCanvas.height = outputSize
  const outCtx = outCanvas.getContext('2d')
  if (!outCtx) {
    throw new Error('Trình duyệt không hỗ trợ xuất ảnh đại diện.')
  }

  outCtx.imageSmoothingEnabled = true
  outCtx.imageSmoothingQuality = 'high'
  outCtx.drawImage(
    tempCanvas,
    pixelCrop.x,
    pixelCrop.y,
    pixelCrop.width,
    pixelCrop.height,
    0,
    0,
    outputSize,
    outputSize,
  )

  return outCanvas
}

export async function createCroppedPreviewDataUrl(
  imageSrc: string,
  pixelCrop: Area,
  rotationDeg = 0,
): Promise<string> {
  const canvas = await renderCroppedCanvas(imageSrc, pixelCrop, rotationDeg, 128)
  return canvas.toDataURL('image/webp', 0.85)
}

export async function createCroppedAvatarFile(
  imageSrc: string,
  pixelCrop: Area,
  rotationDeg = 0,
): Promise<File> {
  const canvas = await renderCroppedCanvas(imageSrc, pixelCrop, rotationDeg, OUTPUT_AVATAR_SIZE)

  const blob = await new Promise<Blob | null>((resolve) => {
    canvas.toBlob((result) => resolve(result), 'image/webp', 0.92)
  })

  if (blob) {
    return new File([blob], 'avatar.webp', { type: 'image/webp' })
  }

  const fallbackBlob = await new Promise<Blob | null>((resolve) => {
    canvas.toBlob((result) => resolve(result), 'image/png')
  })

  if (!fallbackBlob) {
    throw new Error('Không thể tạo tệp ảnh đại diện sau khi cắt.')
  }

  return new File([fallbackBlob], 'avatar.png', { type: 'image/png' })
}
