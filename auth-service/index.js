const express = require('express');
const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json());

// Mock authentication endpoint
app.post('/api/auth/validate', (req, res) => {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        return res.status(401).json({ message: 'Invalid authorization header' });
    }

    const token = authHeader.substring(7);

    // Mock validation - accept any token and return fixed user data
    if (token && token.length > 0) {
        return res.status(200).json({
            userId: '550e8400-e29b-41d4-a716-446655440000',
            role: 'user',
            name: 'Jose Augusto',
            email: 'fejunior5@gmail.com'
        });
    }

    return res.status(401).json({ message: 'Invalid token' });
});

// Health check endpoint
app.get('/health', (req, res) => {
    res.status(200).json({ status: 'healthy' });
});

app.listen(PORT, () => {
    console.log(`Auth Service Mock running on port ${PORT}`);
});
