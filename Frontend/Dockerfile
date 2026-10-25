FROM node:20-alpine

WORKDIR /app

# Copy package files if they exist (optional, for dependencies)
# COPY package*.json ./
# RUN npm install

# Copy frontend files
COPY . .

# Install a simple static server
RUN npm install -g http-server

# Expose port
EXPOSE 80

# Set environment
ENV NODE_ENV=production

# Run static server
CMD ["http-server", ".", "-p", "80", "--cors"]
